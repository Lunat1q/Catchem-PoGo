using System;
using System.Collections.Generic;
using System.Linq;
using C5;
using GeoCoordinatePortable;
using static System.Double;

namespace PoGo.PokeMobBot.Logic.Utils
{
    public static class RoutingUtils
    {
        public static List<GeoCoordinate> GetBestRoute(FortCacheItem startingPokestop,
            IEnumerable<FortCacheItem> pokestopsList, int amountToVisit)
        {
            var map = new MapMatrix(pokestopsList, startingPokestop, amountToVisit);
            var route = map.Optimize();
            var result = new List<GeoCoordinate>();
            foreach (var wp in route)
            {
                result.Add(new GeoCoordinate(wp.Latitude, wp.Longitude));
            }
            return result;
        }
    }

    public class MapMatrix
    {
        public FortCacheItem[] Forts;
        private readonly double[,] _fortsDistance;
        private readonly double[,] _newFortsDistance;
        private bool LimitedVisit => _fortsToVisit > 0;
        private readonly int _fortsToVisit;
        private int[] _opt;
        private double[] _rowMin;
        private double[] _colMin;
        private int _size;
        private bool _nearestNeigh;

        private int[] _currentRoute = new [] {0};

        public MapMatrix(IEnumerable<FortCacheItem> forts, FortCacheItem fortToStart, int fortsToVisit = 0, bool nearestNeigh = true)
        {
            Forts = forts.ToArray();
            _size = Forts.Length;
            _nearestNeigh = nearestNeigh;
            _fortsDistance = new double[_size,_size];
            _newFortsDistance = new double[_size, _size];
            for (var i = 0; i < Forts.Length; i++)
            {
                for (var j = 0; j < Forts.Length; j++)
                {
                    if (i != j)
                    {
                        _fortsDistance[j, i] = _fortsDistance[i, j] = LocationUtils.CalculateDistanceInMeters(Forts[i].Latitude,
                            Forts[i].Longitude,
                            Forts[j].Latitude, Forts[j].Longitude);
                    }
                    else
                    {
                        _fortsDistance[j, i] = PositiveInfinity;
                    }
                }
            }
            if (_fortsToVisit < _size - 1)
                _fortsToVisit = fortsToVisit;

            _opt = LimitedVisit ? new int[_fortsToVisit] : new int[_size];
            _opt[0] = Forts.ToList().IndexOf(fortToStart);
            _opt = ResetOpt(_opt, 1);
        }

        public List<FortCacheItem> OptimizeAll()
        {
            var result = new List<FortCacheItem>();
            _rowMin = new double[_size];
            
            for (var i = 0; i < _size; i++)
            {
                _rowMin[i] = _fortsDistance[i, 0];
                for (var j = 0; j < _size; j++)
                {
                    if (i != j)
                        if (_rowMin[i] > _fortsDistance[i, j])
                            _rowMin[i] = _fortsDistance[i, j];
                }
            }
            for (var i = 0; i < _size; i++)
            {
                for (var j = 0; j < _size; j++)
                {
                    _newFortsDistance[i, j] = _fortsDistance[i, j] - _rowMin[i];
                }
            }
            _colMin = new double[_size];
            for (var i = 0; i < _size; i++)
            {
                _colMin[i] = _newFortsDistance[0, i];
                for (var j = 0; j < _size; j++)
                {
                    if (i != j)
                        if (_colMin[i] > _newFortsDistance[j, i])
                            _colMin[i] = _newFortsDistance[j, i];
                }
            }
            for (var i = 0; i < _size; i++)
            {
                for (var j = 0; j < _size; j++)
                {
                    _newFortsDistance[j, i] = _newFortsDistance[j, i] - _colMin[i];
                }
            }

            //NOT IMPLEMENTED

            return result;
        }

        public List<FortCacheItem> Optimize()
        {
            if (_nearestNeigh)
                return OptimizeNearest();

            return LimitedVisit ? OptimizePart() : OptimizeAll();
        }

        private List<FortCacheItem> OptimizeNearest()
        {
            try
            {
                for (var i = 1; i < _opt.Length; i++)
                {
                    var curDist = MaxValue;
                    for (var j = 0; j < _size; j++)
                    {
                        if (_opt.Contains(j)) continue;
                        if (curDist < _fortsDistance[_opt[i - 1], j]) continue;
                        curDist = _fortsDistance[_opt[i - 1], j];
                        _opt[i] = j;
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
            return _opt.Where(x => x >= 0).Select(t => Forts[t]).ToList();
        }

        private List<FortCacheItem> OptimizePart()
        {
            for (int i = 1; i < _opt.Length; i++)
            {
                for (int j = 0; j < _size; j++)
                {
                    if (!_opt.Contains(j))
                    {
                        _opt[i] = j;
                        break;
                    }
                }
            }
            if (_opt[0] == _size - 1)
                _size--;
            var lastHit = false;
            var opt = new int[_opt.Length];
             _opt.CopyTo(opt, 0);
            var optDist = CalcDist(opt);
            int curIndx = _opt.Length - 1;
            while (!lastHit)
            {
                if (opt[curIndx] == _size - 1 || opt.Contains(_size - 1))
                {
                    curIndx--;
                    if (curIndx == 0) break;
                    opt = ResetOpt(opt, curIndx + 1);
                    continue;
                }
                for (int i = opt[curIndx] + 1; i < _size; i++)
                {
                    if (!opt.Contains(i))
                    {
                        opt[curIndx] = i;
                        if (curIndx == _opt.Length - 1 || _opt.Contains(_size - 1))
                        {
                            var curDist = CalcDist(opt);
                            if (optDist > curDist)
                            {
                                optDist = curDist;
                                opt.CopyTo(_opt,0);
                            }
                        }
                        else
                        {
                            curIndx++;
                            break;
                        }
                    }
                }

                if (opt[1] == _size - 1)
                    lastHit = true;
            }
            return _opt.Select(t => Forts[t]).ToList();
        }

        private double CalcDist(int[] opt)
        {
            double res = 0;
            for (var i = 0; i < opt.Length - 1; i++)
            {
                if (opt[i + 1] == -1) return MaxValue;
                res += _fortsDistance[opt[i], opt[i + 1]];
            }
            return res;
        }

        public int[] ResetOpt(int[] opt, int indx)
        {
            for (var i = indx; i < opt.Length; i++)
            {
                opt[i] = -1;
            }
            return opt;
        }
    }
    

}

