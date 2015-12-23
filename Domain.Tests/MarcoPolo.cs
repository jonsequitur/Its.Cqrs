using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Its.Domain.Tests
{
    public class MarcoPoloPlayerWhoIsIt : EventSourcedAggregate<MarcoPoloPlayerWhoIsIt>
    {
        private readonly HashSet<Guid> playerIds = new HashSet<Guid>();

        public MarcoPoloPlayerWhoIsIt(Guid? id = null) : base(id)
        {
            RecordEvent(new IsIt());
        }

        public MarcoPoloPlayerWhoIsIt(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        public class GameCommandHandler :
            ICommandHandler<MarcoPoloPlayerWhoIsIt, AddPlayer>,
            ICommandHandler<MarcoPoloPlayerWhoIsIt, SayMarco>,
            ICommandHandler<MarcoPoloPlayerWhoIsIt, KeepSayingMarcoOverAndOver>,
            ICommandHandler<MarcoPoloPlayerWhoIsIt, HearPolo>
        {
            private readonly ICommandScheduler<MarcoPoloPlayerWhoIsNotIt> playerScheduler;
            private readonly ICommandScheduler<MarcoPoloPlayerWhoIsIt> itScheduler;

            public GameCommandHandler(
                ICommandScheduler<MarcoPoloPlayerWhoIsIt> itScheduler,
                ICommandScheduler<MarcoPoloPlayerWhoIsNotIt> playerScheduler)
            {
                if (playerScheduler == null)
                {
                    throw new ArgumentNullException("itScheduler");
                }
                if (itScheduler == null)
                {
                    throw new ArgumentNullException("playerScheduler");
                }
                this.playerScheduler = playerScheduler;
                this.itScheduler = itScheduler;
            }

            public async Task EnactCommand(MarcoPoloPlayerWhoIsIt it, AddPlayer command)
            {
                it.RecordEvent(new PlayerJoined
                {
                    PlayerId = command.PlayerId
                });
            }

            public async Task HandleScheduledCommandException(MarcoPoloPlayerWhoIsIt it, CommandFailed<AddPlayer> command)
            {
            }

            public async Task EnactCommand(MarcoPoloPlayerWhoIsIt it, SayMarco command)
            {
                var saidMarco = new SaidMarco();

                it.RecordEvent(saidMarco);

                var scheduleTasks =
                    it.playerIds.Select(playerId => playerScheduler.Schedule(
                        playerId,
                        new MarcoPoloPlayerWhoIsNotIt.SayPolo
                        {
                            IdOfPlayerWhoIsIt = it.Id
                        }, deliveryDependsOn: saidMarco));

                await Task.WhenAll(scheduleTasks);
            }

            public async Task HandleScheduledCommandException(MarcoPoloPlayerWhoIsIt it, CommandFailed<SayMarco> command)
            {
            }

            public async Task EnactCommand(MarcoPoloPlayerWhoIsIt it, HearPolo command)
            {
                it.RecordEvent(new HeardPolo());
            }

            public async Task HandleScheduledCommandException(MarcoPoloPlayerWhoIsIt it, CommandFailed<HearPolo> command)
            {
            }

            public async Task EnactCommand(
                MarcoPoloPlayerWhoIsIt it,
                KeepSayingMarcoOverAndOver command)
            {
                await itScheduler.Schedule(
                    it.Id,
                    new SayMarco(),
                    dueTime: Clock.Now().AddSeconds(1));

                await itScheduler.Schedule(
                    it.Id,
                    new KeepSayingMarcoOverAndOver(),
                    dueTime: Clock.Now().AddSeconds(10));
            }

            public async Task HandleScheduledCommandException(
                MarcoPoloPlayerWhoIsIt it,
                CommandFailed<KeepSayingMarcoOverAndOver> command)
            {
            }
        }

        #region Events

        public class IsIt : Event<MarcoPoloPlayerWhoIsIt>
        {
            public override void Update(MarcoPoloPlayerWhoIsIt it)
            {
            }
        }

        public class PlayerJoined : Event<MarcoPoloPlayerWhoIsIt>
        {
            public Guid PlayerId { get; set; }

            public override void Update(MarcoPoloPlayerWhoIsIt it)
            {
                it.playerIds.Add(PlayerId);
            }
        }

        public class HeardPolo : Event<MarcoPoloPlayerWhoIsIt>
        {
            public override void Update(MarcoPoloPlayerWhoIsIt player)
            {
            }
        }

        public class SaidMarco : Event<MarcoPoloPlayerWhoIsIt>
        {
            public override void Update(MarcoPoloPlayerWhoIsIt it)
            {
            }
        }

        #endregion

        #region Commands

        public class AddPlayer : Command<MarcoPoloPlayerWhoIsIt>
        {
            public Guid PlayerId { get; set; }

            public override bool Authorize(MarcoPoloPlayerWhoIsIt it)
            {
                return true;
            }
        }

        public class SayMarco : Command<MarcoPoloPlayerWhoIsIt>
        {
            public override bool Authorize(MarcoPoloPlayerWhoIsIt it)
            {
                return true;
            }
        }

        public class KeepSayingMarcoOverAndOver : Command<MarcoPoloPlayerWhoIsIt>
        {
            public override bool Authorize(MarcoPoloPlayerWhoIsIt it)
            {
                return true;
            }
        }

        public class HearPolo : Command<MarcoPoloPlayerWhoIsIt>
        {
            public override bool Authorize(MarcoPoloPlayerWhoIsIt it)
            {
                return true;
            }
        }

        #endregion
    }

    public class MarcoPoloPlayerWhoIsNotIt : EventSourcedAggregate<MarcoPoloPlayerWhoIsNotIt>
    {
        public MarcoPoloPlayerWhoIsNotIt(Guid? id = null) : base(id)
        {
        }

        public MarcoPoloPlayerWhoIsNotIt(Guid id, IEnumerable<IEvent> eventHistory) : base(id, eventHistory)
        {
        }

        public MarcoPoloPlayerWhoIsNotIt(ISnapshot snapshot, IEnumerable<IEvent> eventHistory = null) : base(snapshot, eventHistory)
        {
        }

        public string Name { get; private set; }

        public class PlayerCommandHandler :
            ICommandHandler<MarcoPoloPlayerWhoIsNotIt, JoinGame>,
            ICommandHandler<MarcoPoloPlayerWhoIsNotIt, SayPolo>
        {
            private readonly ICommandScheduler<MarcoPoloPlayerWhoIsIt> gameScheduler;

            public PlayerCommandHandler(ICommandScheduler<MarcoPoloPlayerWhoIsIt> gameScheduler)
            {
                if (gameScheduler == null)
                {
                    throw new ArgumentNullException("gameScheduler");
                }
                this.gameScheduler = gameScheduler;
            }

            public async Task EnactCommand(MarcoPoloPlayerWhoIsNotIt player, JoinGame command)
            {
                var joinedGame = new JoinedGame
                {
                    IdOfPlayerWhoIsIt = command.IdOfPlayerWhoIsIt
                };

                player.RecordEvent(joinedGame);

                await gameScheduler.Schedule(command.IdOfPlayerWhoIsIt, new MarcoPoloPlayerWhoIsIt.AddPlayer
                {
                    PlayerId = player.Id
                }, deliveryDependsOn: joinedGame);
            }

            public async Task HandleScheduledCommandException(MarcoPoloPlayerWhoIsNotIt player, CommandFailed<JoinGame> command)
            {
            }

            public async Task EnactCommand(MarcoPoloPlayerWhoIsNotIt player, SayPolo command)
            {
                await gameScheduler.Schedule(
                    command.IdOfPlayerWhoIsIt,
                    new MarcoPoloPlayerWhoIsIt.HearPolo());
            }

            public async Task HandleScheduledCommandException(MarcoPoloPlayerWhoIsNotIt player, CommandFailed<SayPolo> command)
            {
            }
        }

        public class PlayerSnapshotCreator : ICreateSnapshot<MarcoPoloPlayerWhoIsNotIt>
        {
            public ISnapshot CreateSnapshot(MarcoPoloPlayerWhoIsNotIt aggregate)
            {
                var snapshot = new Snapshot
                {
                    Name = aggregate.Name
                };
                aggregate.InitializeSnapshot(snapshot);
                return snapshot;
            }
        }

        public class Snapshot : ISnapshot
        {
            public string Name { get; set; }

            public Guid AggregateId { get; set; }
            public long Version { get; set; }
            public DateTimeOffset LastUpdated { get; set; }
            public string AggregateTypeName { get; set; }
            public BloomFilter ETags { get; set; }
        }

        #region Events

        public class Created : Event<MarcoPoloPlayerWhoIsNotIt>
        {
            public string Name { get; set; }

            public override void Update(MarcoPoloPlayerWhoIsNotIt player)
            {
                player.Name = Name;
            }
        }

        public class JoinedGame : Event<MarcoPoloPlayerWhoIsNotIt>
        {
            public Guid IdOfPlayerWhoIsIt { get; set; }

            public override void Update(MarcoPoloPlayerWhoIsNotIt player)
            {
            }
        }

        public class HeardMarco : Event<MarcoPoloPlayerWhoIsNotIt>
        {
            public override void Update(MarcoPoloPlayerWhoIsNotIt player)
            {
            }
        }

        #endregion

        #region Commands

        public class JoinGame : Command<MarcoPoloPlayerWhoIsNotIt>
        {
            public Guid IdOfPlayerWhoIsIt { get; set; }

            public override bool Authorize(MarcoPoloPlayerWhoIsNotIt player)
            {
                return true;
            }
        }

        public class SayPolo : Command<MarcoPoloPlayerWhoIsNotIt>
        {
            public override bool Authorize(MarcoPoloPlayerWhoIsNotIt player)
            {
                return true;
            }

            public Guid IdOfPlayerWhoIsIt { get; set; }
        }

        #endregion
    }

}