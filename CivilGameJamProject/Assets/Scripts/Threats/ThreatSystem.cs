using System.Collections.Generic;
using UnityEngine;

namespace CivilGame.Threats
{
    public enum ZoneType
    {
        Sea,        // Море (Left Window)
        Mountains,  // Горы (Left Window)
        Forest,     // Лес (Right Window)
        Field       // Поле / Степь (Right Window)
    }

    public enum ThreatType
    {
        Drought,    // Засуха (3 стадии: Желтый -> Желтый -> Красный)
        Wildfire,   // Пожар (3 стадии: Желтый -> Желтый -> Красный)
        Landslide,  // Оползень (2 стадии: Желтый -> Красный)
        Tsunami     // Цунами (2 стадии: Желтый -> Красный)
    }

    public enum SquadType
    {
        Irrigators,         // Оросители (Засуха 1-2 этапы)
        Lumberjacks,        // Лесорубы (Пожар 1 этап)
        Firefighters,       // Пожарные (Пожар 2 этап)
        RescueHelicopter,   // Спасательный вертолет (Пожар и Оползень на критических этапах)
        MountainClimbers,   // Альпинисты (Оползень 1 этап)
        Divers              // Дайверы (Цунами 1 этап)
    }

    [System.Serializable]
    public class ThreatDefinition
    {
        public ThreatType type;
        public string displayName;
        public int maxStages; // 2 или 3 стадии
        public List<ZoneType> allowedZones;

        public ThreatDefinition(ThreatType type, string displayName, int maxStages, params ZoneType[] allowedZones)
        {
            this.type = type;
            this.displayName = displayName;
            this.maxStages = maxStages;
            this.allowedZones = new List<ZoneType>(allowedZones);
        }
    }

    [System.Serializable]
    public class ActiveThreat
    {
        public ThreatDefinition definition;
        public int currentStage; // 1 .. maxStages

        public bool IsFinalStage => currentStage >= definition.maxStages;

        public ActiveThreat(ThreatDefinition definition, int startStage = 1)
        {
            this.definition = definition;
            this.currentStage = startStage;
        }

        public string GetStageDescription()
        {
            switch (definition.type)
            {
                case ThreatType.Drought:
                    if (currentStage == 1) return "Этап 1: Пожелтение листьев";
                    if (currentStage == 2) return "Этап 2: Сухие ветки и трещины";
                    return "Этап 3: Критическая засуха!";

                case ThreatType.Wildfire:
                    if (currentStage == 1) return "Этап 1: Дымок над кронами";
                    if (currentStage == 2) return "Этап 2: Очаги открытого пламени";
                    return "Этап 3: Верховой пожар!";

                case ThreatType.Landslide:
                    if (currentStage == 1) return "Этап 1: Подземные толчки, осыпание камней";
                    return "Этап 2: Сход породы! Немедленная угроза!";

                case ThreatType.Tsunami:
                    if (currentStage == 1) return "Этап 1: Резкий отлив воды";
                    return "Этап 2: Гигантская волна на горизонте!";

                default:
                    return $"Стадия {currentStage}";
            }
        }
    }

    [System.Serializable]
    public class ZoneModel
    {
        public ZoneType zoneType;
        public string zoneName;
        public bool isDestroyed;
        public ActiveThreat activeThreat;
        public int immuneUntilDay;
        public bool isFrozenForNextDay;

        public ZoneModel(ZoneType zoneType, string zoneName)
        {
            this.zoneType = zoneType;
            this.zoneName = zoneName;
            this.isDestroyed = false;
            this.activeThreat = null;
            this.immuneUntilDay = 0;
            this.isFrozenForNextDay = false;
        }

        public Color GetIndicatorColor()
        {
            if (isDestroyed)
            {
                // Черный (регион уничтожен)
                return new Color(0.08f, 0.08f, 0.09f, 0.92f);
            }

            if (activeThreat != null)
            {
                if (activeThreat.IsFinalStage)
                {
                    // Красный (последняя критическая стадия)
                    return new Color(0.92f, 0.18f, 0.18f, 0.85f);
                }
                else
                {
                    // Желтый (1-я или 2-я стадия предпосылок)
                    return new Color(0.95f, 0.82f, 0.14f, 0.82f);
                }
            }

            // Прозрачный (угрозы нет)
            return new Color(1f, 1f, 1f, 0f);
        }
    }
}
