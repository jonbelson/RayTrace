// Copyright (c) 2026 Jonathan Belson
// Licensed under the MIT License — use freely, keep this notice.
// SPDX-License-Identifier: MIT
// Full terms: see LICENSE in the project root.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayTrace
{
    // Helper class for Quaternion rotations.
    // Note that for a right-handed coordinate system, a +ve rotation
    //  follows the right hand rule.
    public struct Quaternion
    {
        public float X, Y, Z, W;

        public Quaternion(float w, float x, float y, float z)
        {
            (X, Y, Z, W) = (x, y, z, w);
        }

        public Quaternion(SVector axis, float angle)
        {
            float sa = (float) Math.Sin(angle/2);

            X = axis.I * sa;
            Y = axis.J * sa;
            Z = axis.K * sa;

            W = (float) Math.Cos(angle / 2);
        }

        public Quaternion GetInverse()
        {
            return new Quaternion(W, -X, -Y, -Z);
        }

        // Rotate a vector by this quaternion.
        // Follows left hand rule.
        public SVector Rotate(SVector v)
        {
            Quaternion qi = GetInverse();
            Quaternion p = new (0, v.I, v.J, v.K);
            // qi*p*q
            Quaternion pq = p * this;
            Quaternion qipq = qi * pq;

            return new SVector(qipq.X, qipq.Y, qipq.Z, 0);
        }

        public static Quaternion operator*(Quaternion q1, Quaternion q2)
        {
            return new Quaternion(
                q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z,   // W

                q1.W*q2.X + q1.X*q2.W + q1.Y*q2.Z - q1.Z*q2.Y,  // X
                q1.W*q2.Y + q1.Y*q2.W + q1.Z*q2.X - q1.X*q2.Z,  // Y
                q1.W*q2.Z + q1.Z*q2.W + q1.X*q2.Y - q1.Y*q2.X  // Z
                );
        }
    }
}
