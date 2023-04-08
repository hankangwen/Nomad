﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ToolType { Default = 0, Axe = 1, Pick = 2, Sword = 3 }
public class Tool : Item
{
    Animator m_Animator;
    public List<Collider> m_HaveHit;
    public ToolType toolType = ToolType.Default;
    public int damage = 3;
    public float damageResetDelay = 0.5f;
    private bool canDealDamage = false;

    void Start()
    {
        m_HaveHit = new List<Collider>();
    }
    private void Update()
    {
        if (canDealDamage && !m_Animator.GetBool("Attacking"))
        {
            canDealDamage = false;
        }
    }

    public override void OnEquipped(GameObject character)
    {
        base.OnEquipped(character);
        m_Animator = character.GetComponentInChildren<Animator>();
    }

    public override void OnUnequipped()
    {
        base.OnUnequipped();
    }

    void OnTriggerStay(Collider other)
    {
        if (isEquipped && m_Animator.GetBool("Attacking") && canDealDamage)
        {
            if (m_HaveHit.Contains(other))
            {
                return;
            }
            else
            {
                m_HaveHit.Add(other);
            }
            try
            {
                HealthManager hm = other.gameObject.GetComponent<HealthManager>();
                hm.TakeDamage(damage, toolType, other.bounds.ClosestPoint(transform.position + transform.up * 2));
            }
            catch
            {
                Debug.Log("Error");
            }

            try
            {
                SourceObject so = other.gameObject.GetComponent<SourceObject>();
                so.TakeDamage(damage, toolType, other.bounds.ClosestPoint(transform.position + transform.up * 2));
            }
            catch
            {
                Debug.Log("Error");
            }
        }
    }

    public void Hit()
    {
        canDealDamage = true;
    }

}
