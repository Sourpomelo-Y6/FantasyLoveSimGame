using System;
using System.Collections.Generic;
using System.Linq;

namespace FantasyLoveSim.EditorTools
{
    public static class RequiredSkillIdSyncService
    {
        public static bool ApplyIfSpecified(List<string> target, IEnumerable<string> source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null)
            {
                return false;
            }

            target.Clear();
            target.AddRange(Normalize(source));
            return true;
        }

        public static List<string> Normalize(IEnumerable<string> values)
        {
            return (values ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
