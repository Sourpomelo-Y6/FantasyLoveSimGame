#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BattleSkillProfileIntegrationTests
{
    private const string HeroineId = "__BattleSkillProfileTest";
    private const string AssetPath = "Assets/Resources/Heroines/" + HeroineId + "Profile.asset";
    private string outputFolder;

    [SetUp]
    public void SetUp()
    {
        AssetDatabase.DeleteAsset(AssetPath);
        outputFolder = Path.Combine(Path.GetFullPath("Temp"), "BattleSkillProfileIntegrationTests");
        if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
        Directory.CreateDirectory(outputFolder);
    }

    [TearDown]
    public void TearDown()
    {
        AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.Refresh();
        if (Directory.Exists(outputFolder)) Directory.Delete(outputFolder, true);
    }

    [Test]
    public void ImportProfile_DistinguishesMissingAndEmptyBattleSkillsAndClampsValues()
    {
        HeroineProfileData profile = CreateProfile();
        profile.battleSkills.Add(Skill("existing", SkillEffectType.Heal, 3));

        HeroineAssetImporter.ApplyProfileJsonForTests(profile, "{\"heroineId\":\"" + HeroineId + "\"}");
        Assert.That(profile.battleSkills.Select(skill => skill.skillId), Is.EqualTo(new[] { "existing" }));

        HeroineAssetImporter.ApplyProfileJsonForTests(profile, "{\"heroineId\":\"" + HeroineId + "\",\"battleSkills\":[]}");
        Assert.That(profile.battleSkills, Is.Empty);

        string json = "{\"heroineId\":\"" + HeroineId + "\",\"battleSkills\":[" +
            "{\"skillId\":\" skill_b \",\"displayName\":\" Skill B \",\"effectType\":\"FutureEffect\",\"target\":\"FutureTarget\",\"cost\":-3,\"power\":-4,\"affectedStat\":\"FutureStat\",\"statusDurationTurns\":0,\"useChancePercent\":120,\"priority\":-2,\"maxUsesPerBattle\":0}," +
            "{\"skillId\":\"skill_a\",\"effectType\":\"Heal\",\"target\":\"Player\",\"cost\":2,\"affectedStat\":\"Defense\",\"statusDurationTurns\":4,\"useChancePercent\":80}," +
            "{\"skillId\":\"SKILL_B\",\"effectType\":\"Damage\"}," +
            "{\"skillId\":\" \",\"effectType\":\"Damage\"}]}";
        HeroineAssetImporter.ApplyProfileJsonForTests(profile, json);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        profile = AssetDatabase.LoadAssetAtPath<HeroineProfileData>(AssetPath);

        Assert.That(profile.battleSkills.Select(skill => skill.skillId), Is.EqualTo(new[] { "skill_b", "skill_a" }));
        HeroineBattleSkillData first = profile.battleSkills[0];
        Assert.That(first.displayName, Is.EqualTo("Skill B"));
        Assert.That(first.effectType, Is.EqualTo(SkillEffectType.Damage));
        Assert.That(first.target, Is.EqualTo(HeroineSkillTarget.Enemy));
        Assert.That(first.affectedStat, Is.EqualTo(SkillBattleStat.Attack));
        Assert.That(first.cost, Is.Zero);
        Assert.That(first.power, Is.EqualTo(-4));
        Assert.That(first.statusDurationTurns, Is.EqualTo(1));
        Assert.That(first.useChancePercent, Is.EqualTo(100));
        Assert.That(first.priority, Is.EqualTo(-2));
        Assert.That(first.maxUsesPerBattle, Is.Zero);
    }

    [Test]
    public void ExportProfile_WritesAllBattleSkillFieldsWithoutDuplicateIds()
    {
        HeroineProfileData profile = CreateProfile();
        HeroineBattleSkillData first = Skill(" skill_b ", SkillEffectType.Buff, 4);
        first.displayName = " Skill B ";
        first.target = HeroineSkillTarget.Player;
        first.power = -3;
        first.affectedStat = SkillBattleStat.Defense;
        first.statusDurationTurns = 5;
        first.useChancePercent = 85;
        first.priority = -2;
        first.maxUsesPerBattle = 0;
        profile.battleSkills.Add(first);
        profile.battleSkills.Add(Skill("skill_a", SkillEffectType.Heal, 2));
        profile.battleSkills.Add(Skill("SKILL_B", SkillEffectType.Damage, 9));
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        HeroineUnityDataExporter.HeroineUnityExportReport report = new HeroineUnityDataExporter.HeroineUnityExportReport();
        HeroineUnityDataExporter.ExportProfile(profile, outputFolder, report);
        ProfileExportFile exported = JsonUtility.FromJson<ProfileExportFile>(
            File.ReadAllText(Path.Combine(outputFolder, "heroine_profile_from_unity.json")));

        Assert.That(exported.battleSkills.Select(skill => skill.skillId), Is.EqualTo(new[] { "skill_b", "skill_a" }));
        BattleSkillExport firstExport = exported.battleSkills[0];
        Assert.That(firstExport.displayName, Is.EqualTo("Skill B"));
        Assert.That(firstExport.effectType, Is.EqualTo("Buff"));
        Assert.That(firstExport.target, Is.EqualTo("Player"));
        Assert.That(firstExport.cost, Is.EqualTo(4));
        Assert.That(firstExport.power, Is.EqualTo(-3));
        Assert.That(firstExport.affectedStat, Is.EqualTo("Defense"));
        Assert.That(firstExport.statusDurationTurns, Is.EqualTo(5));
        Assert.That(firstExport.useChancePercent, Is.EqualTo(85));
        Assert.That(firstExport.priority, Is.EqualTo(-2));
        Assert.That(firstExport.maxUsesPerBattle, Is.Zero);
        Assert.That(report.profileExported, Is.True);
    }

    private static HeroineProfileData CreateProfile()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Heroines");
        HeroineProfileData profile = ScriptableObject.CreateInstance<HeroineProfileData>();
        profile.heroineId = HeroineId;
        profile.battleSkills = new List<HeroineBattleSkillData>();
        AssetDatabase.CreateAsset(profile, AssetPath);
        return profile;
    }

    private static HeroineBattleSkillData Skill(string id, SkillEffectType effect, int cost)
    {
        return new HeroineBattleSkillData { skillId = id, effectType = effect, cost = cost };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, separator), path.Substring(separator + 1));
    }

    [Serializable]
    private sealed class ProfileExportFile
    {
        public List<BattleSkillExport> battleSkills;
    }

    [Serializable]
    private sealed class BattleSkillExport
    {
        public string skillId;
        public string displayName;
        public string effectType;
        public string target;
        public int cost;
        public int power;
        public string affectedStat;
        public int statusDurationTurns;
        public int useChancePercent;
        public int priority;
        public int maxUsesPerBattle;
    }
}
#endif
