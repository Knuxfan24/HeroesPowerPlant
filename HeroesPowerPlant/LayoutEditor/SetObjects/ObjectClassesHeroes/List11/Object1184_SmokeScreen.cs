﻿using SharpDX;

namespace HeroesPowerPlant.LayoutEditor
{
    public class Object1184_SmokeScreen : SetObjectHeroes
    {
        public override void CreateTransformMatrix()
        {
            transformMatrix = IsUpsideDown ? Matrix.RotationY(MathUtil.Pi) : Matrix.Identity *
                DefaultTransformMatrix();

            CreateBoundingBox();
        }

        public int ModelNumber
        {
            get => ReadInt(4);
            set => Write(4, value);
        }

        public float Speed
        {
            get => ReadFloat(8);
            set => Write(8, value);
        }

        public bool IsUpsideDown
        {
            get => ReadByte(12) != 0;
            set => Write(12, (byte)(value ? 1 : 0));
        }
    }
}
