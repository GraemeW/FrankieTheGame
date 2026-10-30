namespace Frankie.Combat.Editor
{
    public readonly struct SkillListEntry
    {
        public readonly Skill skill;
        public readonly string text;

        private SkillListEntry(Skill skill, string text)
        {
            this.skill = skill;
            this.text = text;
        }

        public bool isHeader => skill == null;

        public static SkillListEntry Header(string text) => new(null, text); // Note:  A group header has no skill (its text is the group name + count)
        public static SkillListEntry Entry(Skill skill) => new(skill, skill.name);
    }
}
