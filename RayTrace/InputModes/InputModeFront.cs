// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace RayTrace
{
    internal class InputModeFront : InputMode
    {
        private /*readonly*/ OrthographicCamera _camera;

        private System.Windows.Point _clickPoint;
        private Point3D _clickPoint3D;

        private bool _dragging = false;

        public InputModeFront(OrthographicCamera camera)
        {
            _camera = camera;
        }

        public override void DoMouseWheel(int delta)
        {
            double StepSize = 0.2; 

            double offset = (delta >= 1) ? StepSize : -StepSize;

            _camera.Width -= offset;
        }

        public override void DoMouseMove(System.Windows.Point p)
        {
            bool ctrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            bool shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if (!_dragging)
            {
                _clickPoint = p;
                //_dragging = true;

                return;
            }

            double scale = 0.2;

            double xDiff = scale*(double)(p.X - _clickPoint.X);
            double yDiff = scale*(double)(p.Y - _clickPoint.Y);

            var pos = _camera.Position;

            //double newX = pos.X - xDiff;
            //double newY = pos.Y + yDiff;
            double newX = _clickPoint3D.X - xDiff;
            double newY = _clickPoint3D.Y + yDiff;

            newX = Math.Min(newX, 25);
            newX = Math.Max(newX, -25);
            newY = Math.Min(newY, 25);
            newY = Math.Max(newY, -25);

            _camera.Position = new(newX, newY, pos.Z);

            Debug.WriteLine(_camera.Position);
        }

        public override void DoLButtonDown(System.Windows.Point p)
        {
            _clickPoint = p;
            _clickPoint3D = _camera.Position;

            _dragging = true;
        }

        public override void DoLButtonUp(System.Windows.Point p)
        {
            _dragging = false;
        }

    }
}
