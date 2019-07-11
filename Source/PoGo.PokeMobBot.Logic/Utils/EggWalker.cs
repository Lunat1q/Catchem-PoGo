#region using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PoGo.PokeMobBot.Logic.Extensions;
using PoGo.PokeMobBot.Logic.State;
using PoGo.PokeMobBot.Logic.Tasks;
using POGOProtos.Inventory.Item;

#endregion

namespace PoGo.PokeMobBot.Logic.Utils
{
    public class EggWalker : PropertyNotification
    {
        private double _checkInterval;
        private readonly ISession _session;
        public bool Inited;

        public ObservableCollection<PokeEgg> Eggs = new ObservableCollection<PokeEgg>();

        private double _distanceTraveled;

        public EggWalker(ISession session)
        {
            _session = session;
        }

        public async Task InitEggWalker(CancellationToken cancellationToken)
        {
            if (_session == null) return;
            Inited = true;
            await UseIncubatorsTask.Execute(_session, cancellationToken, Eggs);
            _distanceTraveled = 0;
            double eggMin = 1;
            if (Eggs != null && Eggs.Count > 0)
            {
                eggMin = Eggs.Min(x => x.DistanceLeft);
            }
            _checkInterval = Math.Min(1000, eggMin * 1000 + 100);
        }

        public async Task ApplyDistance(double distanceTraveled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_session.LogicSettings.UseEggIncubators)
                return;

            foreach (var egg in Eggs.Where(x=>x.InsideIncubator))
            {
                egg.WalkedDistance += distanceTraveled / 1000;
            }

            _distanceTraveled += distanceTraveled;
            if (_distanceTraveled > _checkInterval)
            {
                await InitEggWalker(cancellationToken);
            }
        }
    }

    public class PokeEgg : PropertyNotification
    {
        private const string StatusTextProp = "DistanceStatusText";
        private double _distance;
        private double _walkedDistance;
        private double _targetDistance;
        private double _distanceDone;

        private ItemId _incubatorType;

        public double DistanceLeft
        {
            get
            {
                if (_targetDistance - _walkedDistance > 0)
                    return _targetDistance - _walkedDistance;
                return _distance;
            }
        }

        public double DistanceDone
        {
            get { return _distanceDone; }
            set
            {
                _distanceDone = value;
                OnPropertyChanged();
            }
        }

        private ulong _eggId;

        public double Distance
        {
            get { return _distance; }
            set
            {
                _distance = value;
                OnPropertyChanged();
            }
        }

        public double WalkedDistance
        {
            get { return _walkedDistance; }
            set
            {
                _walkedDistance = value;
                OnPropertyChanged();

                DistanceDone = Distance - DistanceLeft;

                OnPropertyChangedByName(StatusTextProp);
            }
        }

        public ulong PokemonUidInside { get; set; }

        public bool InsideIncubator => !string.IsNullOrEmpty(EggIncubatorId);


        public string EggIncubatorId
        {
            get { return _eggIncubatorId; }
            set
            {
                _eggIncubatorId = value;
                OnPropertyChanged();
                OnPropertyChangedByName("InsideIncubator");
                OnPropertyChangedByName(StatusTextProp);
            }
        }

        public ulong EggId
        {
            get { return _eggId; }
            set
            {
                _eggId = value;
                OnPropertyChanged();
            }
        }

        public double TargetDistance
        {
            get { return _targetDistance; }
            set
            {
                _targetDistance = value;
                OnPropertyChanged();
                OnPropertyChangedByName(StatusTextProp);
            }
        }

        public ItemId IncubatorType
        {
            get { return _incubatorType; }
            set
            {
                _incubatorType = value;
                OnPropertyChanged();
            }
        }

        public string DistanceStatusText => $"{DistanceDone.ToString("N2")} / {Distance.ToString("N2")}";

        private string _eggIncubatorId;
    }
}