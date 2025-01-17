﻿using UnityEngine;

[CreateAssetMenu(menuName = "PluggableAI/Decisions/TargetRange")]

public class ActorTargetInRange : Decision
{

    public override bool Decide(StateController controller)
    {
        return TargetInRange(controller);
    }

    private bool TargetInRange(StateController controller)
    {
        if (controller.target == null) return false;

        float dis = Vector3.Distance(controller.target.position, controller.transform.position);

        if (dis <= controller.enemyStats.attackRange)
        {
            if (!controller.currentState.ToString().Contains("AttackActorTarget")) controller.attackCoolDown = 0;
            return true;
        }

        return false;
    }
}
