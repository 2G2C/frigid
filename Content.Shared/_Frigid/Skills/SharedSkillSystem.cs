using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Frigid.Skills;

public abstract class SharedSkillSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<string, SkillDataPrototype> _skillToPrototype = new();

    private readonly List<string> _publicSkills = new();

    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedSkillsComponent, ComponentInit>(OnComponentInit);

        _sawmill = Logger.GetSawmill("skills");
        _sawmill.Level = LogLevel.Info;

        LoadPrototypes();
        _prototypeManager.PrototypesReloaded += HandlePrototypesReloaded;
    }

    private void OnComponentInit(EntityUid uid, SharedSkillsComponent component, ComponentInit args)
    {
        base.Initialize();
        component.Skills.Clear();
        foreach(var skillIdentifier in _publicSkills)
        {
            RetrieveSkillDataPrototype(skillIdentifier, out var prototype);
            if (prototype == null)
                continue;

            var skillData = new SkillStruct(skillIdentifier, prototype.DefaultLevel, prototype.MaxLevel, prototype.DefaultXP, prototype.MaxExperience, prototype.DisplayInSkills);
            component.Skills.Add(skillData);
        }
    }

    private protected void HandlePrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        LoadPrototypes();
    }

    public bool RetrieveSkillDataPrototype(string identifier, [NotNullWhen(true)] out SkillDataPrototype? skill)
    {
        if(_skillToPrototype.TryGetValue(identifier, out var skillOrNot))
        {
            skill = skillOrNot;
            return true;
        }
        skill = null;
        return false;
    }

    private protected void LoadPrototypes()
    {
        _skillToPrototype.Clear();
        _publicSkills.Clear();
        foreach(var skill in _prototypeManager.EnumeratePrototypes<SkillDataPrototype>())
        {
            if(_skillToPrototype.ContainsKey(skill.Name))
            {
                Logger.ErrorS("skills",
                    "Found skill with duplicate SkillDataPrototype Name {0} - all skills must have" +
                    " a unique prototype, this one will be skipped", skill.Name);
            }
            else
            {
                _sawmill.Log(LogLevel.Info, "Added skill prototype with {0} name", skill.Name);
                _skillToPrototype.Add(skill.Name, skill);
                if (skill.DisplayInSkills)
                    _publicSkills.Add(skill.Name);
            }
        }
    }

    [Serializable, NetSerializable]
    public struct SkillStruct
    {
        public string Name { get; set; }
        public bool DisplayInMenu { get; set; }
        public ushort Level { get; set; }
        public ushort MaxLevel { get; set; }
        public ushort Experience { get; set; }
        public ushort MaxExperience { get; set; }

        public SkillStruct(string name, ushort level, ushort maxLevel, ushort exp, ushort maxExp, bool display)
        {
            Name = name;
            Level = level;
            Experience = exp;
            MaxExperience = maxExp;
            MaxLevel = maxLevel;
            DisplayInMenu = display;
        }
    }
}
