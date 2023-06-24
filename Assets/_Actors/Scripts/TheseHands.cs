using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheseHands : MonoBehaviour
{
    Animator m_Animator;
    GameObject m_HansOwner;
    CharacterStats stats;
    public TheseHands partner;
    [HideInInspector]
    public List<Collider> m_HaveHit;
    private bool canDealDamage = false;
    ActorEquipment ae;
    void Start()
    {
        stats = GetComponentInParent<CharacterStats>();
        m_Animator = GetComponentInParent<Animator>();
        m_HansOwner = m_Animator.transform.parent.gameObject;
        ae = m_HansOwner.GetComponent<ActorEquipment>();
        partner = ae.m_TheseHandsArray[0].gameObject.name != gameObject.name ? ae.m_TheseHandsArray[0] : ae.m_TheseHandsArray[1];
    }
    private void Update()
    {
        if (canDealDamage && !m_Animator.GetBool("Attacking"))
        {
            canDealDamage = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!ae.hasItem)
        {
            if (m_Animator.GetBool("Attacking") && m_Animator.GetBool("CanHit"))
            {
                if (m_HaveHit.Contains(other) || partner.m_HaveHit.Contains(other))
                {
                    return;
                }
                else
                {
                    m_HaveHit.Add(other);
                    partner.m_HaveHit.Add(other);
                }
                try
                {
                    HealthManager hm = other.gameObject.GetComponent<HealthManager>();
                    hm.TakeHit(1 + stats.attack, ToolType.Hands, transform.position, m_HansOwner);
                    return;
                }
                catch (System.Exception ex)
                {
                    //Debug.Log(ex);
                }
                try
                {
                    SourceObject so = other.gameObject.GetComponent<SourceObject>();
                    so.TakeDamage(1 + stats.attack, ToolType.Hands, transform.position, m_HansOwner);
                    return;
                }
                catch (System.Exception ex)
                {
                    //error?
                }
            }
        }
    }
    public void Hit()
    {
        canDealDamage = true;
    }
}
