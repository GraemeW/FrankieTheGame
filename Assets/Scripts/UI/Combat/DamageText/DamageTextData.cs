namespace Frankie.Combat.UI
{
    public class DamageTextData
    {
        public readonly DamageTextType damageTextType;
        public readonly float amount;
        public readonly string information;

        public DamageTextData(DamageTextType damageTextType)
        {
            this.damageTextType = damageTextType;
            amount = 0f;
            information = "";
        }

        public DamageTextData(DamageTextType damageTextType, float amount)
        {
            this.damageTextType = damageTextType;
            this.amount = amount;
            information = "";
        }

        public DamageTextData(DamageTextType damageTextType, string information)
        {
            this.damageTextType = damageTextType;
            amount = 0f;
            this.information = information;
        }
    }
}
