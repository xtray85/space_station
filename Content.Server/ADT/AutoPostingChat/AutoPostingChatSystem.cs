using Content.Server.Administration.Commands;
using Content.Server.Popups;
using Content.Shared.Popups;
using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.ADT.AutoPostingChat;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Timers;
using System.ComponentModel;
using System.Linq;
//using System.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
public sealed class AutoPostingChatSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    private System.Timers.Timer _speakTimer = new();
    private System.Timers.Timer _emoteTimer = new();
    private readonly Random _random = new Random(); 
    //private static readonly Random _random = new Random();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoPostingChatComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AutoPostingChatComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<AutoPostingChatComponent, ComponentShutdown>(ComponentRemove);
    }

    private void ComponentRemove(EntityUid uid, AutoPostingChatComponent component, ComponentShutdown args)
    {
        _speakTimer.Stop();
        _emoteTimer.Stop();
        _speakTimer.Dispose(); // освобождаем ресурсы
        _emoteTimer.Dispose();
    }

    /// <summary>
    /// On death removes active comps.
    /// </summary>
    private void OnMobState(EntityUid uid, AutoPostingChatComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead || component == null)
        {
            RemComp<AutoPostingChatComponent>(uid);
        }
    }

    private void OnComponentStartup(EntityUid uid, AutoPostingChatComponent component, ComponentStartup args)
    {
        if (component == null)
        {
            Log.Debug("AutoPostingChatComponent отсутствует на сущности с UID: " + uid);
            return;
        }

        _speakTimer.Interval = component.RandomIntervalSpeak ? _random.Next(1000, 30001) : component.SpeakTimerRead;
        _speakTimer.Elapsed += (sender, eventArgs) =>
        {
            if (component.PostingMessageSpeak != null)
            {
                _chat.TrySendInGameICMessage(uid, component.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
            }
            _speakTimer.Interval = component.RandomIntervalSpeak ? _random.Next(1000, 30001) : component.SpeakTimerRead;
        };

        _emoteTimer.Interval = component.RandomIntervalEmote ? _random.Next(1000, 30001) : component.EmoteTimerRead;
        _emoteTimer.Elapsed += (sender, eventArgs) =>
        {
            if (component.PostingMessageEmote != null)
            {
                _chat.TrySendInGameICMessage(uid, component.PostingMessageEmote, InGameICChatType.Emote, ChatTransmitRange.Normal);
            }
            _emoteTimer.Interval = component.RandomIntervalEmote ? _random.Next(1000, 30001) : component.EmoteTimerRead;
        };

        _speakTimer.Start();
        _emoteTimer.Start();
    }
}