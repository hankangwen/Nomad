using UnityEngine;
using System.Collections.Generic;

public class AttackBehavior : StateMachineBehaviour
{
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    bool hasTurnedOffCooldown = false;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.SetBool("CoolDown", true);
        hasTurnedOffCooldown = false;

        Debug.Log("### cool down true");
        // Set the "LeftAttack" parameter to true when the state is entered
        //animator.SetBool("LeftAttack", true);
        TheseHands[] theseHands = animator.gameObject.GetComponentsInChildren<TheseHands>();
        foreach (TheseHands th in theseHands)
        {
            th.m_HaveHit = new List<Collider>();
        }
        ActorEquipment ae = animator.gameObject.GetComponentInParent<ActorEquipment>();
        if (ae != null && ae.hasItem)
        {
            try
            {
                Tool item = ae.equippedItem.GetComponent<Item>() as Tool;
                item.m_HaveHit = new List<Collider>();
            }
            catch
            {
                Debug.Log("**Item not a tool**");
            }
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // If the animation is 75% complete
        if (stateInfo.normalizedTime >= .5f && hasTurnedOffCooldown == false)
        {
            // Set the "LeftAttack" parameter to false
            animator.SetBool("CoolDown", false);
            hasTurnedOffCooldown = true;
            Debug.Log("### cool down false");

        }
        if (stateInfo.normalizedTime >= 1f)
        {
            // Set the "LeftAttack" parameter to false
            animator.SetBool("Attacking", false);
        }
    }
}
