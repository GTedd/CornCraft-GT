#nullable enable

namespace CraftSharp.Control
{
    public class AttackStatus
    {
        // Player attack data
        public float AttackCooldown = 0F;
        public int AttackStage = 0;

        public bool CausingDamage = false;

        public override string ToString()
        {

            return $"Atk Cd:\t{AttackCooldown}\nAtk St:\t{AttackStage}";
        }

    }
}