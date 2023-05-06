using Pathfinding;
using UnityEngine;

public class EndHit : StateMachineBehaviour
{
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.normalizedTime >= .8f)
        {
            animator.SetBool("TakeHit", false);
        }
    }
}
