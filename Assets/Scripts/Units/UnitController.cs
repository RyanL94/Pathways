﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : PathfindingAgent
{
    [Header("Unit Controller")]
    [SerializeField] internal int hitPoints = 1;
    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private float attackDelay = 1;
    [SerializeField] private GameObject projectile = null;
    [SerializeField] private bool respawns = false;

    internal string team;
    internal string enemyTeam;
    internal int totalHitPoints;
    internal UnitController target;
    internal HealthBar healthBar;

    private const float meleeAttackRange = 1.5f;
    private const float attackAnimationDuration = 0.3f;
    private const float respawnDelay = 10;
    private float nextAttack = 0;
    private Vector3 size;
    private Vector3 spawnPosition;
    private new Collider collider;

    protected override void Awake()
    {
        base.Awake();
        IdentifyTeam();
        totalHitPoints = hitPoints;
        collider = GetComponent<Collider>();
        size = collider.bounds.size;
    }

    void Start()
    {
        healthBar = HUD.instance.CreateHealthBar(this);
        spawnPosition = transform.position;
    }

    void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
    }

    // Inflict the given amount of damage to the unit.
    public void TakeDamage(int damage)
    {
        // Update hit points
        hitPoints -= damage;
        hitPoints = Mathf.Max(0, hitPoints);
        animator.SetFloat("health", hitPoints);
        
        // Death check
        if (hitPoints <= 0)
        {
            Disable();
            collider.enabled = false;
            if (!respawns)
            {
                Destroy(gameObject, 5);
            }
            else
            {
                StartCoroutine(Respawn(respawnDelay));
            }
        }
    }

    // Make the unit fire a projectile toward the target position.
    public void FireAt(Vector2 targetPosition)
    {
        if (!activated) return;
        RotateTowards(targetPosition, FireForward);
    }

    // Make the unit attack the target enemy in melee range.
    public void Attack(UnitController targetEnemy)
    {
        if (!activated) return;
        target = targetEnemy;
        MoveTo(targetEnemy.transform.position, size.z + meleeAttackRange, PerformAttack);
    }

    // Make the unit fire a projectile towards their forward direction.
    private void FireForward()
    {
        if (Time.time >= nextAttack)
        {
            StartCoroutine(AnimateAttack());
            var offset = (Vector3.up * size.y * 0.5f) + (transform.forward * size.z / 2);
            var instance = Instantiate(projectile, transform.position + offset, transform.rotation);
            instance.GetComponent<Projectile>().target = enemyTeam;
            nextAttack = Time.time + attackDelay;
        }
    }

    // Damage the current target of the unit.
    // This assumes that the target is in melee range.
    private void PerformAttack()
    {
        if (Time.time >= nextAttack)
        {
            StartCoroutine(AnimateAttack());
            StartCoroutine(PerformAttackCoroutine());
        }
        else
        {
            target = null;
        }
    }

    // Coroutine which adds a delay to the melee attack to simulate the wind up.
    private IEnumerator PerformAttackCoroutine()
    {
        yield return new WaitForSeconds(attackAnimationDuration);
        if (target != null)
        {
            target.TakeDamage(meleeDamage);
            target = null;
            nextAttack = Time.time + attackDelay;
        }
    }

    // Coroutine which sets the attacking parameter of the animator to true for a brief duration.
    private IEnumerator AnimateAttack()
    {
        animator.SetBool("attacking", true);
        yield return new WaitForSeconds(attackAnimationDuration);
        animator.SetBool("attacking", false);
    }

    // Make the unit respawn after the given delay.
    private IEnumerator Respawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.position = spawnPosition;
        hitPoints = totalHitPoints;
        animator.SetFloat("health", hitPoints);
        collider.enabled = true;
        activated = true;
    }

    // Return the team that the unit belongs to by looking at the tag.
    private void IdentifyTeam()
    {
        if (tag.StartsWith("blue"))
        {
            team = "blue";
            enemyTeam = "red";
        }
        else if (tag.StartsWith("red"))
        {
            team = "red";
            enemyTeam = "blue";
        }
        else
        {
            Debug.LogError("Invalid team tag on current unit");
        }
    }
}