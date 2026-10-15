using System.Collections.Generic;
using System.Linq;
using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Services 
{
    public static class NeedStore
    {
        // Liste som holder på alle registrerte behov i minnet
        public static List<ResourceNeed> Needs { get; set; } = new List<ResourceNeed>();
        private static int _nextId = 1;

        // Legg til eller oppdater et behov
        public static void AddOrUpdate(ResourceNeed need)
        {
            if (need.Id == 0)
            {
                need.Id = _nextId++;
                Needs.Add(need);
            }
            else
            {
                var index = Needs.FindIndex(n => n.Id == need.Id);
                if (index != -1)
                {
                    Needs[index] = need;
                }
            }
        }

        // Hent et spesifikt behov basert på ID
        public static ResourceNeed? GetById(int id)
        {
            return Needs.FirstOrDefault(n => n.Id == id);
        }

        // Slett et behov
        public static void Remove(int id)
        {
            var need = GetById(id);
            if (need != null)
            {
                Needs.Remove(need);
            }
        }
    }
}