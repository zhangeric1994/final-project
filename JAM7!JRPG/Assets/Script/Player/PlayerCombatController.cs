﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum StatsType : int
{
    // may move this part to stats manager or game manager
    Power,
    Dexterity,
    Wisdom
}

public enum statsType : int
{
    WalkSpeed,
    JumpPower,
    MaxHp,
    CriticalChance,
    CriticalDamage,
    BaseDamge,
    attackSpeed,

}


public enum PlayerCombatState
{
    Default = 0,
    OnGround,
    InAir,
}

public class PlayerCombatController : MonoBehaviour
{
    //[Header("Stats")]
    //[SerializeField] public float walkSpeed;
    //[SerializeField] public int jumpPower;
    //[SerializeField] public int maxHp;

    //[SerializeField] public float criticalChance;
    //[SerializeField] public float criticalDamageFactor;

    //[SerializeField] public float damageFactor;
    //[SerializeField] public float attackSpeedFactor;


    public Transform weaponHolder;

    public int PlayerID { get; private set; }

    private PlayerCombatState currentState;

    private Vector2 aimmingDirection;

    private SpriteRenderer renderer;
    private Rigidbody2D rb2d;
    private Animator anim;

    private TilemapCollider2D groundCollider;
    private TilemapCollider2D wallCollider;
    private ContactFilter2D groundContactFilter;
    private ContactFilter2D rightWallContactFilter;
    private ContactFilter2D leftWallContactFilter;
    private int jumpCounter = 0;
    private bool isInAir;

    private float hp;
    private int magazine;

    private float invulnerableInterval = 0.3f;
    private float lastHit;

    public FMODUnity.StudioEventEmitter PlayerFootstepEmitter;

    [SerializeField] private GameObject shield;

    public CombatManager Combat;

    private Vector3 defaultScale;

    public bool okToAttack;

    public EventOnDataChange2<float> OnHpChange { get; private set; }

    public GameObject cam;

    private float lastInput;

    private HashSet<Loot> loots = new HashSet<Loot>();


    public Player Avatar
    {
        get
        {
            return Player.GetPlayer(PlayerID);
        }
    }

    public PlayerCombatState CurrentState
    {
        // this allowed to triggger codes when the state switched
        get
        {
            return currentState;
        }

        private set
        {
            if (value == currentState)
            {
                // nothing
            }
            else
            {
                switch (currentState)
                {
                    case PlayerCombatState.InAir:
                        isInAir = false;
                        jumpCounter = 0;
                        break;
                }

                PlayerCombatState previousState = currentState;
                currentState = value;

                Debug.Log(LogUtility.MakeLogStringFormat("PlayerController", "{0} --> {1}", previousState, currentState));

                switch (currentState)
                {
                    case PlayerCombatState.InAir:
                        rb2d.AddForce(Avatar.GetStatistic(StatisticType.JumpPower) * Vector2.up);
                        jumpCounter = 1;
                        break;
                }
            }
        }
    }

    public float Hp
    {
        get
        {
            return hp;
        }

        private set
        {
            if (value != hp)
            {
                hp = value;

                OnHpChange.Invoke(hp, Avatar.GetStatistic(StatisticType.MaxHp));
            }
        }
    }


    private PlayerCombatController() { }

    public void Initialize(int id)
    {
        PlayerID = id;

        OnHpChange = new EventOnDataChange2<float>();
    }

    private void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        groundContactFilter = new ContactFilter2D();
        groundContactFilter.SetNormalAngle(30, 150);

        rightWallContactFilter = new ContactFilter2D();
        rightWallContactFilter.SetNormalAngle(170, 190);

        leftWallContactFilter = new ContactFilter2D();
        leftWallContactFilter.SetNormalAngle(-10, 10);

        defaultScale = transform.localScale;
        hp = Avatar.GetStatistic(StatisticType.MaxHp);

        CurrentState = PlayerCombatState.OnGround;
        okToAttack = true;

        PlayerFootstepEmitter = null;
    }

    public void GetCamera()
    {
        var cameras = GameObject.FindGameObjectsWithTag("MainCamera");
        foreach (var camera in cameras)
        {
            if (camera.GetComponent<ForwardCamera>().index == PlayerID)
            {
                cam = camera;
            }
        }
    }

    private void Update()
    {
        if (cam == null)
        {
            GetCamera();
            return;
        }

        if (groundCollider == null)
        {
            groundCollider = GameObject.FindGameObjectWithTag("Ground").GetComponent<TilemapCollider2D>();
            return;
        }

        if (wallCollider == null)
        {
            wallCollider = GameObject.FindGameObjectWithTag("Wall").GetComponent<TilemapCollider2D>();
            return;
        }

        if (PlayerFootstepEmitter == null)
        {
            var player = GameObject.Find("WeaponHolder");
            PlayerFootstepEmitter = player.GetComponent<FMODUnity.StudioEventEmitter>();
        }

        switch (currentState)
        {
            case PlayerCombatState.OnGround:
                {
                    float x = Input.GetAxis("Horizontal");
                    float y = Input.GetAxis("Vertical");

                    if (okToAttack && x != 0 || y != 0)
                    {
                        transform.localScale = x < 0 ? new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z)
                            : new Vector3(defaultScale.x, defaultScale.y, defaultScale.z);
                    }

                    //anim.SetFloat("Speed",Mathf.Abs(h)+Mathf.Abs(v));

                    if (okToAttack && x > 0)
                    {
                        float walkSpeed = Avatar.GetStatistic(StatisticType.WalkSpeed);
                        rb2d.velocity = new Vector2(walkSpeed, rb2d.velocity.y);
                        anim.SetFloat("Speed", walkSpeed);
                        PlayerFootstepEmitter.SetParameter("Speed", 1);
                        PlayerFootstepEmitter.SetParameter("Grass", 1);
                        //renderer.flipX = false;
                    }
                    else if (okToAttack && x < 0)
                    {
                        float walkSpeed = Avatar.GetStatistic(StatisticType.WalkSpeed);
                        rb2d.velocity = new Vector2(-walkSpeed, rb2d.velocity.y);
                        anim.SetFloat("Speed", walkSpeed);
                        PlayerFootstepEmitter.SetParameter("Speed", 1);
                        PlayerFootstepEmitter.SetParameter("Grass", 1);
                        //renderer.flipX = true;
                    }
                    else
                    {
                        rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                        anim.SetFloat("Speed", 0f);
                        PlayerFootstepEmitter.SetParameter("Speed", 0);
                        PlayerFootstepEmitter.SetParameter("Grass", 0);
                    }

                    if (Input.GetButtonDown("Jump") && lastInput != Time.time + 10f)
                    {
                        CurrentState = PlayerCombatState.InAir;
                        lastInput = Time.time;
                        FMOD.Studio.EventInstance jumpSound = FMODUnity.RuntimeManager.CreateInstance("event:/Combat/Jump");
                        jumpSound.start();
                    }

                    if (Input.GetButtonDown("Pick") && lastInput != Time.time + 10f)
                    {
                        Loot();
                        lastInput = Time.time;
                    }


                    if (Input.GetButtonDown("Fire") && okToAttack && lastInput != Time.time + 10f)
                    {
                        okToAttack = false;
                        anim.Play(weaponHolder.GetComponentInChildren<Weapon>().getAnimationName());
                        anim.speed = Avatar.GetStatistic(StatisticType.AttackSpeed);
                        lastInput = Time.time;
                        PlayWeaponSound();
                       
                    }

                    if (Input.GetButtonDown("Vertical") && lastInput != Time.time + 10f)
                    {
                        GameObject door = GameObject.FindWithTag("Door");
                        if (door != null)
                        {
                            if ((transform.position - door.transform.position).magnitude < 2.0f)
                            {
                                ClearLoots();
                                Combat.endCombat();
                            }
                        }
                    }

                }
                break;


            case PlayerCombatState.InAir:
                {
                    PlayerFootstepEmitter.SetParameter("Speed", 0);
                    PlayerFootstepEmitter.SetParameter("Grass", 0);
                    bool isTouchingGround = rb2d.IsTouching(groundCollider, groundContactFilter);

                    if (isInAir)
                    {
                        if (isTouchingGround)
                            CurrentState = PlayerCombatState.OnGround;
                    }
                    else if (!isTouchingGround)
                        isInAir = true;

                    if (rb2d.IsTouching(wallCollider, rightWallContactFilter))
                    {
                        if (Input.GetButtonDown("Jump") && jumpCounter != 2)
                        {
                            jumpCounter = 2;
                            rb2d.velocity = Vector2.zero;
                            rb2d.AddForce(0.5f * Avatar.GetStatistic(StatisticType.JumpPower) * new Vector2(-1, 2));
                            transform.localScale = new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z);
                            FMOD.Studio.EventInstance jumpSound = FMODUnity.RuntimeManager.CreateInstance("event:/Combat/Jump");
                            jumpSound.start();
                        }

                        float x = Input.GetAxis("Horizontal");
                        float y = Input.GetAxis("Vertical");

                        if (okToAttack && (x != 0 || y != 0))
                        {
                            transform.localScale = x < 0 ? new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z)
                                : new Vector3(defaultScale.x, defaultScale.y, defaultScale.z);
                        }

                        if (okToAttack && x < 0)
                        {
                            float walkSpeed = Avatar.GetStatistic(StatisticType.WalkSpeed);
                            rb2d.velocity = new Vector2(-walkSpeed, rb2d.velocity.y);
                            anim.SetFloat("Speed", walkSpeed);
                            //renderer.flipX = true;
                        }
                        else
                        {
                            rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                            anim.SetFloat("Speed", 0);
                        }
                    }
                    else if (rb2d.IsTouching(wallCollider, leftWallContactFilter))
                    {
                        if (Input.GetButtonDown("Jump") && jumpCounter != 3)
                        {
                            jumpCounter = 3;
                            rb2d.velocity = Vector2.zero;
                            rb2d.AddForce(0.5f * Avatar.GetStatistic(StatisticType.JumpPower) * new Vector2(1, 2));
                            transform.localScale = new Vector3(defaultScale.x, defaultScale.y, defaultScale.z);
                        }

                        float x = Input.GetAxis("Horizontal");
                        float y = Input.GetAxis("Vertical");

                        if (okToAttack && (x != 0 || y != 0))
                        {
                            transform.localScale = x < 0 ? new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z)
                                : new Vector3(defaultScale.x, defaultScale.y, defaultScale.z);
                        }

                        if (okToAttack && x > 0)
                        {
                            float walkSpeed = Avatar.GetStatistic(StatisticType.WalkSpeed);
                            rb2d.velocity = new Vector2(walkSpeed, rb2d.velocity.y);
                            anim.SetFloat("Speed", walkSpeed);
                        }
                        else
                        {
                            rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                            anim.SetFloat("Speed", 0);
                        }
                    }
                    else
                    {
                        float x = Input.GetAxis("Horizontal");
                        float y = Input.GetAxis("Vertical");

                        if (okToAttack && (x != 0 || y != 0))
                        {
                            transform.localScale = x < 0 ? new Vector3(-defaultScale.x, defaultScale.y, defaultScale.z)
                                : new Vector3(defaultScale.x, defaultScale.y, defaultScale.z);
                        }

                        if (okToAttack && x > 0)
                        {
                            float walkSpeed = Avatar.GetStatistic(StatisticType.WalkSpeed);
                            rb2d.velocity = new Vector2(walkSpeed, rb2d.velocity.y);
                            anim.SetFloat("Speed", walkSpeed);
                        }
                        else if (okToAttack && x < 0)
                        {
                            float walkSpeed = Avatar.GetStatistic(StatisticType.WalkSpeed);
                            rb2d.velocity = new Vector2(-walkSpeed, rb2d.velocity.y);
                            anim.SetFloat("Speed", walkSpeed);
                        }
                        else
                        {
                            rb2d.velocity = new Vector2(0, rb2d.velocity.y);
                            anim.SetFloat("Speed", 0);
                        }
                    }
                }
                break;
        }
    }

    public void Hurt(float rawDamage)
    {
        if (lastHit + invulnerableInterval < Time.time)
        {
            //Hp--;
            //todo fix hp add
            if (Hp < 0)
            {
                //dead
            }
            else
            {
                Avatar.ApplyDamage(rawDamage);
                FMOD.Studio.EventInstance hurtSound = FMODUnity.RuntimeManager.CreateInstance("event:/Combat/Injured");
                hurtSound.start();
                StartCoroutine(HurtDelay());
            }

            lastHit = Time.time;
        }

    }

    //    public void setStates(statsType type, float num)
    //    {
    //        switch (type)
    //        {
    //            case statsType.attackSpeed:
    //                attackSpeedFactor += num;
    //                break;
    //            
    //            case statsType.JumpPower:
    //                jumpPower += (int)num;
    //                break;
    //            
    //            case statsType.CriticalChance:
    //                criticalChance += num;
    //                break;
    //            
    //            case statsType.CriticalDamage:
    //                criticalDamageFactor += num;
    //                break;
    //            
    //            case statsType.MaxHp:
    //                maxHp += (int)num;
    //                hp += (int)num;
    //                break;
    //            
    //            case statsType.BaseDamge:
    //                damageFactor += num;
    //                break;
    //            
    //            case statsType.WalkSpeed:
    //                walkSpeed += num;
    //                break;
    //            
    //        }
    //        
    //    }


    public void activeAttackBox()
    {
        var box = gameObject.GetComponentInChildren<BoxCollider2D>();
        if (!box.isActiveAndEnabled)
        {
            box.enabled = true;
        }

        gameObject.GetComponentInChildren<Weapon>().setAttackId();
    }

    public void inactiveAttackBox()
    {
        var box = gameObject.GetComponentInChildren<BoxCollider2D>();
        if (box.isActiveAndEnabled)
        {
            box.enabled = false;
        }
        
         if (gameObject.GetComponentInChildren<Weapon>().type == WeaponType.Hammer)
        {
            ForwardCamera._instance.Shaking(0.05f, 0.1f);
            gameObject.GetComponentInChildren<Weapon>().ShowVFX();
        }
    }


    public void resetAttack()
    {

        anim.Play("Knight_Idle");
        StartCoroutine(resetAtk());
    }

    IEnumerator resetAtk()
    {
        yield return new WaitForSeconds(0.02f);
        okToAttack = true;
        anim.speed = 1;
    }

    public void pauseAtkAnim(float hitStop)
    {
        anim.speed /= 10;
        StartCoroutine(resetAtkAnim(hitStop));
    }

    IEnumerator resetAtkAnim(float hitStop)
    {
        yield return new WaitForSeconds(hitStop);
        anim.speed = Avatar.GetStatistic(StatisticType.AttackSpeed);
    }

    //    private void Ability()
    //    {
    //        lastAbility = Time.unscaledTime;
    //        inAbility = true;
    //        switch (type)
    //        {
    //            case HeroType.Knight:
    ////                float temp = weaponHolder.GetComponentInChildren<Gun>().reloadSpeed;
    //
    //                StartCoroutine(resetReloadDelay(temp));
    //                break;
    //            case HeroType.Nurse:
    //                GunManager._instance.generateHealDrop(transform);
    //                inAbility = false;
    //                break;
    //            case HeroType.Fat:
    //                shield.SetActive(true);
    //                StartCoroutine(resetShieldDelay());
    //                break;
    //        }
    //    }


    //    public void levelUp()
    //    {
    //        switch (type)
    //        {
    //            case HeroType.Knight:
    //                dexterity++;
    //                break;
    //            case HeroType.Nurse:
    //                wisdom++;
    //                break;
    //            case HeroType.Fat:
    //                maxHp++;
    //                break;
    //        }
    //        Hp = maxHp;
    //    }

    private void Loot()
    {
        if (loots.Count > 0)
        {
            float minDistance = float.MaxValue;
            Loot targetLoot = null;

            float x = gameObject.transform.position.x;
            foreach (Loot loot in loots)
            {
                float distance = Mathf.Abs(loot.gameObject.transform.position.x - x);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    targetLoot = loot;
                }
            }

            loots.Remove(targetLoot);
            FMOD.Studio.EventInstance pickSound = FMODUnity.RuntimeManager.CreateInstance("event:/Combat/Pick");
            pickSound.start();

            switch (targetLoot.Type)
            {
                case LootType.Weapon:
                    weaponHolder.GetComponentInChildren<Weapon>().Destroy();
                    break;
            }

            targetLoot.Trigger(this);
        }

        //var items = FindObjectsOfType<Loot>();
        //foreach (var item in items)
        //{
        //    float distanceToItem = (item.transform.position - transform.position).sqrMagnitude;
        //    if (distanceToItem <= 0.1f && item.gameObject.activeInHierarchy)
        //    {
        //        if (item.Type() == LootType.Weapon)
        //        {
        //            weaponHolder.GetComponentInChildren<Weapon>().Destroy();
        //            item.GetComponent<Loot>().Trigger(this);
        //        }
        //        else if (item.Type() == LootType.Potion)
        //        {
        //            //Hp++;
        //            item.GetComponent<Loot>().Trigger(this);
        //            //TODO do UI update
        //        }
        //        else if (item.Type() == LootType.Item)
        //        {
        //            switch (item.getStatsType())
        //            {
        //                //TODO: Inventory


        //                //case statsType.WalkSpeed:
        //                //    walkSpeed += 0.05f;
        //                //    break;


        //                //case statsType.JumpPower:
        //                //    jumpPower += 10;
        //                //    break;


        //                //case statsType.MaxHp:
        //                //    maxHp += 1;
        //                //    hp += 1;
        //                //    break;


        //                //case statsType.CriticalChance:
        //                //    criticalChance += 5f;
        //                //    break;


        //                //case statsType.CriticalDamage:
        //                //    criticalDamageFactor += 0.1f;
        //                //    break;


        //                //case statsType.BaseDamge:
        //                //    damageFactor += 0.1f;
        //                //    break;


        //                //case statsType.attackSpeed:
        //                //    attackSpeedFactor += 0.1f;
        //                //    break;
        //            }

        //            item.GetComponent<Loot>().Trigger(this);
        //            //TODO do UI update
        //        }
        //    }
        //}
    }

    public void ClearLoots()
    {
        loots.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Loot loot = other.GetComponent<Loot>();

        if (loot)
        {
            switch (loot.Type)
            {
                case LootType.Item:
                    if (!loot.triggered)
                    {
                        Avatar.Loot(loot);
                        loot.triggered = true;
                        string text = "";
                        switch (loot.Id)
                        {
                            case 1:
                                text = "Movement Speed increased by 5%!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                            case 2:
                                text = "Jump Power increased by 10!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                            case 3:
                                text = "Max Health increased by 1!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                            case 4:
                                text = "Critical Chance increased by 5%!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                            case 5:
                                text = "Critical Damage increased by 10%!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                            case 6:
                                text = "Base Damage increased by 10%!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                            case 7:
                                text = "Attack Speed increased by 10%!";
                                GUIManager.Singleton.CreateFloatingText(text, transform.position);
                                break;
                        }
                        Destroy(loot.gameObject);
                        FMOD.Studio.EventInstance lootSound = FMODUnity.RuntimeManager.CreateInstance("event:/Combat/AutoLoot");
                        lootSound.start();
                    }
                    break;
                case LootType.Potion:
                    if (!loot.triggered)
                    {
                        loot.Trigger(this);
                        string text = "Hp + 3!";
                        GUIManager.Singleton.CreateFloatingText(text, transform.position);
                        Destroy(loot.gameObject);
                        FMOD.Studio.EventInstance potionSound = FMODUnity.RuntimeManager.CreateInstance("event:/Combat/Potion");
                        potionSound.start();
                    }
                    break;
                default:
                    loots.Add(loot);
                    break;
            }
        }
    }

    //    IEnumerator resetReloadDelay(float val)
    //    {
    ////        //simple animation
    ////        weaponHolder.GetComponentInChildren<Gun>().reloadSpeed = 0;
    ////        weaponHolder.GetComponentInChildren<Gun>().fireRate *= 0.5f;
    ////        yield return new WaitForSeconds(4f);
    ////        weaponHolder.GetComponentInChildren<Gun>().reloadSpeed = val;
    ////        weaponHolder.GetComponentInChildren<Gun>().fireRate /= 0.5f;
    ////        inAbility = false;
    //    }


    private void OnTriggerExit2D(Collider2D other)
    {
        Loot loot = other.GetComponent<Loot>();

        if (loot)
        {
            switch (loot.Type)
            {
                case LootType.Item:
                    break;


                default:
                    loots.Remove(loot);
                    break;
            }
        }
    }

    //    IEnumerator resetReloadDelay(float val)
    //    {
    ////        //simple animation
    ////        weaponHolder.GetComponentInChildren<Gun>().reloadSpeed = 0;
    ////        weaponHolder.GetComponentInChildren<Gun>().fireRate *= 0.5f;
    ////        yield return new WaitForSeconds(4f);
    ////        weaponHolder.GetComponentInChildren<Gun>().reloadSpeed = val;
    ////        weaponHolder.GetComponentInChildren<Gun>().fireRate /= 0.5f;
    ////        inAbility = false;
    //    }

    IEnumerator resetShieldDelay()
    {
        //simple animation
        yield return new WaitForSeconds(6f);
        shield.SetActive(false);
    }

    IEnumerator HurtDelay()
    {
        //simple animation
        anim.SetBool("Hurt", true);
        renderer.color = Color.gray;
        yield return new WaitForSeconds(0.1f);
        renderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        renderer.color = Color.gray;
        yield return new WaitForSeconds(0.1f);
        renderer.color = Color.white;
        anim.SetBool("Hurt", false);
    }

    private void PlayWeaponSound()
    {
        WeaponType type = weaponHolder.GetComponentInChildren<Weapon>().type;

        if (type == WeaponType.GiantSword)
        {
            FMOD.Studio.EventInstance attackSound = FMODUnity.RuntimeManager.CreateInstance("event:/Attack/Sword");
            //attackSound.setPitch()
            weaponHolder.GetComponentInChildren<Weapon>().attackSound = attackSound;
            attackSound.setParameterValue("Hit", 0);
            attackSound.start();
        }
        else if(type == WeaponType.LightSword)
        {
            FMOD.Studio.EventInstance attackSound = FMODUnity.RuntimeManager.CreateInstance("event:/Attack/StabSword");
            //attackSound.setPitch()
            weaponHolder.GetComponentInChildren<Weapon>().attackSound = attackSound;
            attackSound.setParameterValue("Hit", 0);
            attackSound.start();
        }
        else{
            FMOD.Studio.EventInstance attackSound = FMODUnity.RuntimeManager.CreateInstance("event:/Attack/Hammer");
            //attackSound.setPitch()
            weaponHolder.GetComponentInChildren<Weapon>().attackSound = attackSound;
            attackSound.setParameterValue("Hit", 0);
            attackSound.start();
        }
    }
}
