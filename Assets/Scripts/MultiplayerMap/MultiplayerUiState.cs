using System;
namespace J2.MultiplayerMap
{
    [Serializable] public sealed class ShopOfferView
    {
        public int offerId, spawnTypeId, price, rarity;
        public string name, portraitPath, description;
        public bool canBuy;
    }
    [Serializable] public sealed class PlayerRowView
    {
        public int playerId, arenaIndex, health, maxHealth;
        public string name, portraitPath;
        public bool isLocal;
    }
    [Serializable] public sealed class UnitDetailsView
    {
        public int entityId, stars;
        public string name, portraitPath, type, element, role, description;
        public float health, maxHealth, sp, maxSp, attack, defense, intelligence, speed, range;
        public bool canSell, canDeploy;
    }
    [Serializable] public sealed class UnitLabelView
    {
        public int entityId, stars;
        public float health, maxHealth, sp, maxSp;
        public bool enemy;
    }
    [Serializable] public sealed class MultiplayerUiState
    {
        public string stage, phase, notice, resultTitle, resultDescription, economyDescription;
        public int gold, level, xp, nextLevelXp, rerollPrice, xpPrice, xpAmount, deployed, capacity;
        public float remainingSeconds, phaseDuration;
        public bool canReroll, canBuyXp, showResult, gameOver, canRestart;
        public ShopOfferView[] offers = new ShopOfferView[5];
        public PlayerRowView[] players = Array.Empty<PlayerRowView>();
        public UnitLabelView[] units = Array.Empty<UnitLabelView>();
        public UnitDetailsView details;
        public string[] costProbabilities = new string[5];
    }
}
