using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameEventValidationReport
{
    private readonly List<string> warnings = new List<string>();

    public int EventCount { get; internal set; }
    public int WarningCount => warnings.Count;
    public bool IsValid => warnings.Count == 0;
    public IReadOnlyList<string> Warnings => warnings;

    internal void Warn(string message)
    {
        warnings.Add(message);
    }

    public string CreateSummary()
    {
        return "Game event validation: events=" + EventCount + " / warnings=" + WarningCount;
    }

    public void Log()
    {
        string summary = "[GameEventValidation] " + CreateSummary();
        if (IsValid)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogWarning(summary);
        }

        for (int i = 0; i < warnings.Count; i++)
        {
            Debug.LogWarning("[GameEventValidation] " + warnings[i]);
        }
    }
}

public static class GameEventDataValidator
{
    public static GameEventValidationReport ValidateResources()
    {
        return Validate(
            Resources.LoadAll<GameEventData>("Heroines"),
            Resources.LoadAll<SkillData>("Skills"));
    }

    public static GameEventValidationReport Validate(
        IEnumerable<GameEventData> gameEvents,
        IEnumerable<SkillData> skills)
    {
        GameEventValidationReport report = new GameEventValidationReport();
        HashSet<string> skillIds = BuildSkillIds(skills);

        if (gameEvents == null)
        {
            return report;
        }

        foreach (GameEventData gameEvent in gameEvents)
        {
            if (gameEvent == null)
            {
                continue;
            }

            report.EventCount++;
            ValidateRequiredSkills(gameEvent, skillIds, report);
        }

        return report;
    }

    private static HashSet<string> BuildSkillIds(IEnumerable<SkillData> skills)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        if (skills == null)
        {
            return result;
        }

        foreach (SkillData skill in skills)
        {
            if (skill != null && !string.IsNullOrWhiteSpace(skill.skillId))
            {
                result.Add(skill.skillId);
            }
        }

        return result;
    }

    private static void ValidateRequiredSkills(
        GameEventData gameEvent,
        HashSet<string> skillIds,
        GameEventValidationReport report)
    {
        if (gameEvent.requiredSkillIds == null)
        {
            return;
        }

        string eventLabel = !string.IsNullOrWhiteSpace(gameEvent.eventId)
            ? gameEvent.eventId
            : gameEvent.name;
        HashSet<string> registeredIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < gameEvent.requiredSkillIds.Count; i++)
        {
            string skillId = gameEvent.requiredSkillIds[i];
            if (string.IsNullOrWhiteSpace(skillId))
            {
                report.Warn(eventLabel + " の requiredSkillIds に空の ID があります。");
                continue;
            }

            if (!registeredIds.Add(skillId))
            {
                report.Warn(
                    eventLabel + " の requiredSkillIds が重複しています: " + skillId);
            }

            if (!skillIds.Contains(skillId))
            {
                report.Warn(
                    eventLabel + " が存在しないスキルを要求しています: " + skillId);
            }
        }
    }
}
