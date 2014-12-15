# Warlock Rotation
## Settings

### All Specializations

Some Settings will may only work for _some_ Specializations. It is indicated on the setting here in this document, I could not integrate this into the bot as the settings are already long enough. It's also not possible to hide those Properties for the Specializations which does not need this setting, as it gets inherited from the _Warlock.BaseRotation_ and you can't mask defined Properties in Child-Classes. Settings which are really only feasible within only _one_ Specialization will get defined in their own section.

- **General: Disable OutOfCombat for FishBot**
If this setting is enabled all OutOfCombat Rotations are disabled for the FishBot.
**Beware:** *The bot will not keep any buff or pet active while using FishBot!*

- **Pet: Use Pet**
If this is option is not set, then the bot won't summon a pet as companion and also dismiss manual summoned ones.
**Note:** If Grimorie of Sacrifice is used it _will_ summon a pet to sacrifice it.

- **Pet: Selected Pet**
This option defines the Pet which will get summoned. The following table illustrates the priority list which will get checked from top to bottom for the Auto-Select mode.
If the condition matches it will summon the shown pet and ignore all other possibilities below.

| Condition                            | Pet                 |
|--------------------------------------|---------------------|
| Player has Spell "Demonic Servitude" | Infernal            |
| Player can summon Felguard           | Felguard/Wrathguard |
| Player uses PvPBot                   | Felhunter/Observer  |
| Player is not in Group               | Voidwalker/Voidlord |
| Player has Grimorie of Supremacy     | Felhunter/Observer  |
|                                      | Imp/Fel Imp         |
|--------------------------------------|---------------------|


- **Pet: HealthFunnel Pet HP in %** - (_Demonology and Affliction only_)
This defines the Health of the Pet in % at which the bot will start using Health Funnel to heal it.
It will _not_ do any other Combat Rotation as long as your Pet is below this value, because it is first priority!

- **Pet: HealthFunnel Player HP in %** - (_Demonology and Affliction only_)
This defines the Health of the Player in % until the bot tries to heal the pet. If the Players HP sink below this level it wil cease using Health Funnel until it regained enough Health.

- **DPS: Use Terrorguard/Infernal/Grimorie of Service automatically**
This setting defines if the named DPS cooldowns will be used by the bot automatically. Lets see the condition Table it will use to decide what should be cast. It will cast the spell of first matching condition.

| Condition                                                       | Pet / Ability       |
|-----------------------------------------------------------------|---------------------|
| There are at least 4 Mobs also additionaly the Target is a Boss | Abyssal             |
| or the Target has more Health than the Player and is Elite      | Infernal            |
|-----------------------------------------------------------------|---------------------|
| Target is a Boss                                                | Terrorguard         |
| or the Target has more Health than the Player and is Elite      | Doomguard           |
|-----------------------------------------------------------------|---------------------|
| Target is a Boss                                                |                     |
| or the Target has more Health than the Player and is Elite      | Grimorie of Service |
|-----------------------------------------------------------------|---------------------|


- **DPS: Grimorie of Service Pet**
This lets you define the Pet which will get used when using the Grimorie of Service.

- **DPS: Use Dark Soul automatically**
If this setting is checked it will use DarkSoul automatically on CD if the Target is a Boss or the Target has more Health than the Player and is Elite.

- **PvP: Do Fear**
Use Single-Target Fear on PvP Targets.
The Targets will be banned from being feared again for the configured "Fear Ban Time".
It will also check all enemies in the area and add them to the fear-ban list if they got feared by another Warlock.

- **PvP: Fear Ban Time**
This defines the time a target will get banned from getting single-target feared.

- **Survival: Soulstone yourself if not in group**
If this setting is checked it will use the Soulstone on yourself if you are not in group. If you however are in group it will never use it automatically as it is really hard to define a "right" target automatically.

- **Survival: Use Shadowfury as Interrupt**
If this setting is _checked_ it will save Shadowfury for the very first interruptible cast it can find, regardless of Elite, Boss or anything else. However if the setting is _unchecked_ it will try to use Shadowfury to stun a group of at least 3 enemies.

- **Automatic mana-management: Use Life Tap** - (_Demonology and Affliction only_)
The Bot will use Life Tap if this setting is checked. It will do so the moment the mana drops right below the amount it will regenerate. So in theory you should always be at almost 100%.

- **Automatic mana-management: Life of Player in % until Life Tap gets used** - (_Demonology and Affliction only_)
This setting defines when the bot will cease to use Life Tap. If the Players Health sinks below this value it will not use Life Tap to regain mana anymore.

- **Boss Setting: Percentual factor of a Targets MaxHP in relation to Players MaxHP to be valued as Bossencounter**
This setting (_BossHealthPercentage_) defines a factor the Players MaxHealth will get multiplicated with. If this multiplicated value is lower then the health of the Target it will be count as "Boss" in any condition which checks for a "Boss" Target.
**The used condition is:** _Target.MaxHealth >= Me.MaxHealth * ( BossHealthPercentage / 100f )_

- **Boss Setting: +Level a Target has to have to be valued as Boss encounter**
This setting (_BossLevelIncrease_) defines the Levels which will get added to the Level of the Player. If the Player then is a lower Level than the Target it will be counted as "Boss" in any condition which checks for a "Boss" Target.
**The used condition is:** _Target.Level >= Me.Level + BossLevelIncrease_

- **Boss Setting: Use Dark Soul on Boss only**
If this is checked it will use DarkSoul _only_ if the target gets valued as "Boss"

- **Boss Setting: Use Terrorguard/Infernal/Grimorie of Service on Boss only**
If this is checked it will use the named DPS cooldowns _only_ if the target gets valued as "Boss"

### Affliction only

- None

### Demonology only

- **DPS: Use Hellfire (disable for leveling!)**
If this setting is checked it will use Hellfire if there are at least 4 Enemies in front of the character.
**Note:** *It will only use it if the Mobs are in reach of the Player!*

- **DPS: Minimal Health to do Hellfire in %**
This setting defines the minimum health a player should have to _activate_ Hellfire for one full channeling.
**Beware:** *It will not abort the spell if Players health drops below this setting!*

### Destruction only

- **DPS: Use Havoc on Mobs with HP in %**
This will define how much health the Target should at least have so havoc will be used on it.
It will use Havoc on the healthiest Target available within the set HP range defined by the percentual setting.
**Note:** *If you set this too low it may cause wasted Havoc charges!*

- **DPS: Use Havoc on your Focus (if friendly on its Target)**
If this setting is checked it will use Havoc _always_ on your set "/focus" Target. If this Target is a friendly Player/NPC it will use its Target instead.

## Basic Rotation

TODO

## Affliction Rotation

TODO

## Demonology Rotation

TODO

## Destruction Rotation

TODO
