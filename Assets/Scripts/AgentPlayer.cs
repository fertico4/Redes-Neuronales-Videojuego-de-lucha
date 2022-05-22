using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class AgentPlayer : Agent
{
    //Health
    public int Life = 100;
    [SerializeField] private Slider LifeSlider;
    [SerializeField] private Animator AnimatorControl;
    [Space]
    [Header("Parameters")]
    [SerializeField] private float Speed = 2f;
    [SerializeField] private float PunchDelay = 0.9f;
    [SerializeField] private float HardPunchDelay = 1.8f;
    [SerializeField] private float KickDelay = 1.2f;
    [SerializeField] private float HardKickDelay = 2.4f;
    [SerializeField] private float BlockDelay = 0.6f;
    [SerializeField] private float ComboDelay = 3f;
    [Space]
    [Header("Distances/Damages")]
    [SerializeField] private float PunchDistance = 2f;
    [SerializeField] private int PunchDamage = 5;
    [SerializeField] private int HardPunchDamage = 7;
    [SerializeField] private float KickDistance = 2.5f;
    [SerializeField] private int KickDamage = 8;
    [SerializeField] private int HardKickDamage = 10;
    [SerializeField] private int PunchComboDamage = 13;
    [SerializeField] private int KickComboDamage = 16;
    [SerializeField] private int PunchKickDamage = 18;
    [SerializeField] private float PunchKickDistance = 3f;
    [SerializeField] private int MegaComboDamage = 21;
    [Space]
    [SerializeField] private GameObject otherPlayer;
    [Space]
    [Header("Agent Parameters")]
    [SerializeField] private Vector3 playerSpawnPos;
    [SerializeField] private Vector3 agentSpawnPos;
    [SerializeField] private Transform wallRight;
    [SerializeField] private Transform wallLeft;
    private static int INITIAL_LIFE = 100;
    //Boleanas ataques basicos
    private bool hadPunched = false;
    private bool hadHardPunched = false;
    private bool hadKicked = false;
    private bool hadHardKicked = false;
    private bool hadBlocked = false;
    private bool hadCombo = false;
    private bool canRecibeDamage = true;
    private bool hadAttacked = false;
    private int pointStepCount = 0;

    #region AI
    


    public override void CollectObservations(VectorSensor sensor) //Necesitamos observar donde estamos(3), la vida del agente(1) y la vida del jugador original(1)
    {
        //Posicion del agente (3)
        sensor.AddObservation(transform.position);

        //Vector del agente al jugador (3)
        sensor.AddObservation(otherPlayer.transform.position - transform.position);

        //Distancia del agente al jugador (1)
        Vector3 distanciaAG = otherPlayer.transform.position - transform.position;
        sensor.AddObservation(distanciaAG.magnitude);

        //Distancia con el muro de la derecha (1)
        Vector3 distanciaWallD = wallRight.position - transform.position;
        sensor.AddObservation(distanciaWallD);

        //Distancia con el muro de la izquierda (1)
        Vector3 distanciaWallI = wallLeft.position - transform.position;
        sensor.AddObservation(distanciaWallI);

        //Vida del agente (1)
        sensor.AddObservation(Life);

        //Vida del jugador (1)
        sensor.AddObservation(otherPlayer.GetComponent<Player>().Life);

        //Ataques agente (6)
        sensor.AddObservation(hadPunched);
        sensor.AddObservation(hadHardPunched);
        sensor.AddObservation(hadKicked);
        sensor.AddObservation(hadHardKicked);
        sensor.AddObservation(hadBlocked);
        sensor.AddObservation(hadCombo);

        //Ataques jugador(6)
        sensor.AddObservation(otherPlayer.GetComponent<Player>().hadPunched);
        sensor.AddObservation(otherPlayer.GetComponent<Player>().hadHardPunched);
        sensor.AddObservation(otherPlayer.GetComponent<Player>().hadKicked);
        sensor.AddObservation(otherPlayer.GetComponent<Player>().hadHardKicked);
        sensor.AddObservation(otherPlayer.GetComponent<Player>().hadBlocked);
        sensor.AddObservation(otherPlayer.GetComponent<Player>().hadCombo);
    }
    public override void OnActionReceived(float[] vectorAction) //Acciones que va a hacer: Moverse (3) Ataques (8) y Bloqueo (1)
    {
        
        if (Life <= 0)
        {
            AddReward(-15);
            EndEpisode();
            return;
        }
        //Movimiento
        float horizontal = vectorAction[0];
        switch (horizontal)
        {
            case 0: //Parado
                transform.position += transform.right * 0 * Speed * Time.deltaTime;
                AnimatorControl.SetFloat("Movement", 0);
                GetComponent<Rigidbody>().isKinematic = true;
                break;
            case 1: //Izquierda
                transform.position += transform.right * -1 * Speed * Time.deltaTime;
                AnimatorControl.SetFloat("Movement", -1);
                GetComponent<Rigidbody>().isKinematic = false;
                break;
            case 2: //Derecha
                transform.position += transform.right * 1 * Speed * Time.deltaTime;
                AnimatorControl.SetFloat("Movement", 1);
                GetComponent<Rigidbody>().isKinematic = false;
                break;

        }
        pointStepCount++;

        Debug.Log("Steps count: " + pointStepCount);
        //Combos
        Combos(vectorAction);
        //Puch
        Punch(vectorAction);
        //Hard Punch
        HardPunch(vectorAction);
        //Kick
        Kick(vectorAction);
        //Hard Kick
        HardKick(vectorAction);
        //Bloqueo
        StartCoroutine(Block(vectorAction));
    }

    public override void OnEpisodeBegin()
    {
        transform.position = agentSpawnPos;
        otherPlayer.transform.position = playerSpawnPos;
        Life = INITIAL_LIFE;
        LifeSlider.value = INITIAL_LIFE;
        otherPlayer.GetComponent<Player>().Life = INITIAL_LIFE;
        otherPlayer.GetComponent<Player>().LifeSlider.value = INITIAL_LIFE;
    }

    #endregion
    // Movimiento (0), Punch(1), HardPuch(2), Kick(3), HardKick(4), Block(5)
    // Aumentamos el tamano de los branches por la cantidad de botones que haya para que piense más los ataques - +5
    #region Mechanics
    private void Combos(float[] vectorAction)
    {
        float punch = vectorAction[1];
        float hardpunch = vectorAction[2];
        float kick = vectorAction[3];
        float hardkick = vectorAction[4];

        if (hadAttacked)
            return;
        if (hadCombo && pointStepCount > ComboDelay)
        {
            hadPunched = false;
            hadHardPunched = false;
            hadKicked = false;
            hadHardKicked = false;
            hadCombo = false;
            hadAttacked = false;
        }
        //Combo punch
        if (punch == 0 && hardpunch == 0 && !hadCombo)
        {
            hadPunched = true;
            hadHardPunched = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Combo Punch");
            AttackPlayer(PunchComboDamage, PunchDistance);
            //yield return new WaitForSeconds(ComboDelay);
            pointStepCount = 0;
        }

        //Combo kick
        if (kick == 0 && hardkick == 0 && !hadCombo)
        {
            hadKicked = true;
            hadHardKicked = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Combo Kick");
            AttackPlayer(KickComboDamage, KickDistance);
            //yield return new WaitForSeconds(ComboDelay);
            pointStepCount = 0;
        }

        //Combo punch-kick
        if (punch == 0 && kick == 0 && !hadCombo)
        {
            hadPunched = true;
            hadKicked = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Combo Punch Kick");
            AttackPlayer(PunchKickDamage, PunchKickDistance);
            pointStepCount = 0;
        }

        //Combo Megacombo (hard punch and kick)
        if (hardpunch == 0 && hardkick == 0 && !hadCombo)
        {
            hadHardPunched = true;
            hadHardKicked = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Mega Combo");
            AttackPlayer(MegaComboDamage, PunchKickDistance);
            //yield return new WaitForSeconds(ComboDelay + 1);
            pointStepCount = 0;
        }
    }


    private void Punch(float[] vectorAction)
    {
        if (hadAttacked)
            return;
        if (hadPunched && pointStepCount > PunchDelay)
        {
            hadPunched = false;
            hadAttacked = false;
        }
        if (vectorAction[1] == 0 && !hadPunched)
        {
            hadPunched = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Punch");
            AttackPlayer(PunchDamage, PunchDistance);
            pointStepCount = 0;
        }
    }
    private void HardPunch(float[] vectorAction)
    {
        if (hadAttacked)
            return;
        if (hadHardPunched && pointStepCount > HardPunchDelay)
        {
            hadHardPunched = false;
            hadAttacked = false;
        }
        if (vectorAction[2] == 0 && !hadHardPunched)
        {
            hadHardPunched = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Hard Punch");
            AttackPlayer(HardPunchDamage, PunchDistance);
            pointStepCount = 0;
        }
    }
    private void Kick(float[] vectorAction)
    {
        if (hadAttacked)
            return;
        if (hadKicked && pointStepCount > KickDelay)
        {
            hadKicked = false;
            hadAttacked = false;
        }
        if (vectorAction[3] == 0 && !hadKicked)
        {
            hadKicked = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Kick");
            AttackPlayer(KickDamage, KickDistance);
            pointStepCount = 0;
        }
    }

    private void HardKick(float[] vectorAction)
    {
        if (hadAttacked)
            return;
        if (hadHardKicked && pointStepCount > HardKickDelay)
        {
            hadHardKicked = false;
            hadAttacked = false;
        }
        if (vectorAction[4] == 0 && !hadHardKicked)
        {
            hadHardKicked = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Hard Kick");
            AttackPlayer(HardKickDamage, KickDistance);
            pointStepCount = 0;
        }
    }

    private IEnumerator Block(float[] vectorAction)
    {
        if (vectorAction[5] == 0 && !hadBlocked)
        {
            hadBlocked = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Block");
            while (AnimatorControl.GetCurrentAnimatorStateInfo(0).normalizedTime < AnimatorControl.GetCurrentAnimatorStateInfo(0).length)
            {
                canRecibeDamage = false;
                yield return null;
            }
            canRecibeDamage = true;
            yield return new WaitForSeconds(BlockDelay);
            hadBlocked = false;
            hadAttacked = false;
        }
    }

    #endregion

    public void LifeChanged(int damage)
    {
        if (!canRecibeDamage)
        {
            AddReward(+3); // Recibe premio por haberse protegido
            return;
        }
        Life -= damage;
        LifeSlider.value = Life;
        AnimatorControl.SetTrigger("Hit");
        switch (damage)
        {
            case 5: // Punch
                AddReward(-1);
                break;
            case 7: // Hard Punch
                AddReward(-2);
                break;
            case 8: // Kick
                AddReward(-3);
                break;
            case 10: // Hard Kick
                AddReward(-4);
                break;
            case 13: // Combo Punch
                AddReward(-6);
                break;
            case 16: // Combo Kick
                AddReward(-7);
                break;
            case 18: // Punch Kick
                AddReward(-9);
                break;
            case 21: // MegaCombo
                AddReward(-12);
                break;
        }
    }

    private void AttackPlayer(int damage, float distanceAttack)
    {
        if (Vector3.Distance(transform.position, otherPlayer.transform.position) <= distanceAttack)
        {
            Debug.Log("Ataque: " + damage);
            otherPlayer.GetComponent<Player>().LifeChanged(damage);
        }
        else if(Vector3.Distance(transform.position, otherPlayer.transform.position) <= distanceAttack)
        {
            AddReward(-4);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name == "Wall")
        {
            AddReward(-15); // Quita premio por salirse de los limites
            EndEpisode();
        }
    }

}
