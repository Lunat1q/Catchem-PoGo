using System;
using System.Windows.Controls;
using Catchem.Pages;
using PoGo.PokeMobBot.Logic.Utils;

namespace Catchem.Extensions
{
    internal class PlayerMovement
    {
        private readonly GifImage _moveTop;
        private readonly GifImage _moveDown;
        private readonly GifImage _moveLeft;
        private readonly GifImage _moveRight;
        private readonly GifImage _stay;

        public PlayerMovement()
        {
            var tt = new ToolTip { Content = "Player" };
            _moveTop = new GifImage(Properties.Resources.trainer_top) { Image = {ToolTip = tt} };
            _moveDown = new GifImage(Properties.Resources.trainer_down) { Image = { ToolTip = tt } };
            _moveLeft = new GifImage(Properties.Resources.trainer_left) { Image = { ToolTip = tt } };
            _moveRight = new GifImage(Properties.Resources.trainer_right) { Image = { ToolTip = tt } };
            _stay = new GifImage(Properties.Resources.trainer_stay) { Image = { ToolTip = tt } };
        }

        public MoveDirections CalcDirection(bool moveRequired, double latStep, double lngStep)
        {
            if (!moveRequired || Math.Abs(lngStep) < 1E-17)
                return MoveDirections.Stay;
            var bearing = LocationUtils.DegreeBearing(0, 0, latStep, lngStep);
            if (bearing > 0 && bearing <= 45)
                return MoveDirections.Top;
            if (bearing > 45 && bearing <= 135)
                return MoveDirections.Right;
            if (bearing > 135 && bearing <= 225)
                return MoveDirections.Down;
            if (bearing > 225 && bearing <= 315)
                return MoveDirections.Left;
            return MoveDirections.Top;
        }

        private Image GetImageForDirection(MoveDirections direction)
        {
            switch (direction)
            {
                case MoveDirections.Top:
                    return _moveTop.Image;
                case MoveDirections.Down:
                    return _moveDown.Image;
                case MoveDirections.Left:
                    return _moveLeft.Image;
                case MoveDirections.Right:
                    return _moveRight.Image;
                case MoveDirections.Stay:
                    return _stay.Image;
                default:
                    return _stay.Image;
            }
        }

        public Image GetCurrentImage(bool moveRequired, double latStep, double lngStep)
        {
            var direction = CalcDirection(moveRequired, latStep, lngStep);
            return GetImageForDirection(direction);
        }
    }
}
