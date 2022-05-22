using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private string MoveString = "Horizontal";
    [SerializeField] private string PunchString = "Punch";
    [SerializeField] private string KickString = "Kick";
    [SerializeField] private string BlockString = "Block";
    [SerializeField] private string HardPunchString = "Hard Punch";
    [SerializeField] private string HardKickString = "Hard Kick";
    //Health
    public int Life = 100;
    public Slider LifeSlider;
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
    //Boleanas ataques basicos
    [HideInInspector] public bool hadPunched = false;
    [HideInInspector] public bool hadHardPunched = false;
    [HideInInspector] public bool hadKicked = false;
    [HideInInspector] public bool hadHardKicked = false;
    [HideInInspector] public bool hadBlocked = false;
    [HideInInspector] public bool hadCombo = false;
    private bool canRecibeDamage = true;
    private bool hadAttacked = false;

    private void Update()
    {
        if (Life <= 0)
        {
            Debug.Log("Muere jugador");
            otherPlayer.GetComponent<AgentPlayer>().AddReward(+15);
            otherPlayer.GetComponent<AgentPlayer>().EndEpisode();
            return;
        }
        //Movimiento
        float horizontal = Input.GetAxis(MoveString);
        transform.position += transform.right * horizontal * Speed * Time.deltaTime;
        AnimatorControl.SetFloat("Movement", horizontal);
        if (horizontal != 0) { GetComponent<Rigidbody>().isKinematic = false; }
        if (horizontal == 0) { GetComponent<Rigidbody>().isKinematic = true; }

        if (hadAttacked)
            return;
        //Combos
        StartCoroutine(Combos());
        //Puch
        StartCoroutine(Punch());
        //Hard Punch
        StartCoroutine(HardPunch());
        //Kick
        StartCoroutine(Kick());
        //Hard Kick
        StartCoroutine(HardKick());
        //Bloqueo
        StartCoroutine(Block());
        
    }

    #region Mechanics
    private IEnumerator Combos()
    {
        float punch = Input.GetAxis(PunchString);
        float hardpunch = Input.GetAxis(HardPunchString);
        float kick = Input.GetAxis(KickString);
        float hardkick = Input.GetAxis(HardKickString);

        //Combo punch
        if (punch != 0 && hardpunch != 0 && !hadCombo)
        {
            hadPunched = true;
            hadHardPunched = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Combo Punch");
            AttackPlayer(PunchComboDamage, PunchDistance);
            yield return new WaitForSeconds(ComboDelay);
            hadPunched = false;
            hadHardPunched = false;
            hadCombo = false;
            hadAttacked = false;
        }

        //Combo kick
        if (kick != 0 && hardkick != 0 && !hadCombo)
        {
            hadKicked = true;
            hadHardKicked = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Combo Kick");
            AttackPlayer(KickComboDamage, KickDistance);
            yield return new WaitForSeconds(ComboDelay);
            hadKicked = false;
            hadHardKicked = false;
            hadCombo = false;
            hadAttacked = false;
        }

        //Combo punch-kick
        if (punch != 0 && kick != 0 && !hadCombo)
        {
            hadPunched = true;
            hadKicked = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Combo Punch Kick");
            AttackPlayer(PunchKickDamage, PunchKickDistance);
            yield return new WaitForSeconds(ComboDelay);
            hadPunched = false;
            hadKicked = false;
            hadCombo = false;
            hadAttacked = false;
        }

        //Combo Megacombo (hard punch and kick)
        if (hardpunch != 0 && hardkick != 0 && !hadCombo)
        {
            hadHardPunched = true;
            hadHardKicked = true;
            hadCombo = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Mega Combo");
            AttackPlayer(MegaComboDamage, PunchKickDistance);
            yield return new WaitForSeconds(ComboDelay + 1);
            hadHardPunched = false;
            hadHardKicked = false;
            hadCombo = false;
            hadAttacked = false;
        }
    }


    private IEnumerator Punch()
    {
        if (Input.GetAxis(PunchString) != 0 && !hadPunched) 
        {
            hadPunched = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Punch");
            AttackPlayer(PunchDamage, PunchDistance);
            yield return new WaitForSeconds(PunchDelay);
            hadPunched = false;
            hadAttacked = false;
        }
    }
    private IEnumerator HardPunch()
    {
        if (Input.GetAxis(HardPunchString) != 0 && !hadHardPunched)
        {
            hadHardPunched = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Hard Punch");
            AttackPlayer(HardPunchDamage, PunchDistance);
            yield return new WaitForSeconds(HardPunchDelay);
            hadHardPunched = false;
            hadAttacked = false;
        }
    }
    private IEnumerator Kick()
    {
        if (Input.GetAxis(KickString) != 0 && !hadKicked) 
        {
            hadKicked = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Kick");
            AttackPlayer(KickDamage, KickDistance);
            yield return new WaitForSeconds(KickDelay);
            hadKicked = false;
            hadAttacked = false;
        }
    }

    private IEnumerator HardKick()
    {
        if (Input.GetAxis(HardKickString) != 0 && !hadHardKicked)
        {
            hadHardKicked = true;
            hadAttacked = true;
            AnimatorControl.SetTrigger("Hard Kick");
            AttackPlayer(HardKickDamage, KickDistance);
            yield return new WaitForSeconds(HardKickDelay);
            hadHardKicked = false;
            hadAttacked = false;
        }
    }

    private IEnumerator Block()
    {
        if (Input.GetAxis(BlockString) != 0 && !hadBlocked) 
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
        AgentPlayer agent = otherPlayer.GetComponent<AgentPlayer>();
        if (!canRecibeDamage)
        {
            agent.AddReward(-3);
            return;
        }
        Life -= damage;
        LifeSlider.value = Life;
        AnimatorControl.SetTrigger("Hit");
        switch (damage)
        {
            case 5: // Punch
                agent.AddReward(+1);
                break;
            case 7: // Hard Punch
                agent.AddReward(+2);
                break;
            case 8: // Kick
                agent.AddReward(+3);
                break;
            case 10: // Hard Kick
                agent.AddReward(+4);
                break;
            case 13: // Combo Punch
                agent.AddReward(-6);
                break;
            case 16: // Combo Kick
                agent.AddReward(+7);
                break;
            case 18: // Punch Kick
                agent.AddReward(+9);
                break;
            case 21: // MegaCombo
                agent.AddReward(-12);
                break;
        }
    }

    private void AttackPlayer(int damage, float distanceAttack)
    {
        if (Vector3.Distance(transform.position, otherPlayer.transform.position) <= distanceAttack)
        {
            Debug.Log("Ataque: " + damage);
            otherPlayer.GetComponent<AgentPlayer>().LifeChanged(damage);
        }
    }


}
