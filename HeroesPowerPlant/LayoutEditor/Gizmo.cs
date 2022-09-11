﻿using SharpDX;
using System.Collections.Generic;

namespace HeroesPowerPlant.LayoutEditor
{
    public enum GizmoType
    {
        X,
        Y,
        Z,
    }

    public class Gizmo
    {
        public GizmoType type;
        public bool isSelected;
        private Matrix transformMatrix;

        public Gizmo(GizmoType type)
        {
            this.type = type;

            switch (type)
            {
                case GizmoType.X:
                    renderData.Color = new Vector4(1f, 0f, 0f, 0.4f);
                    break;
                case GizmoType.Y:
                    renderData.Color = new Vector4(0f, 1f, 0f, 0.4f);
                    break;
                case GizmoType.Z:
                    renderData.Color = new Vector4(0f, 0f, 1f, 0.4f);
                    break;
            }
            isSelected = false;
            SetPosition(new BoundingSphere());
        }

        public void SetPosition(BoundingSphere Sphere)
        {
            if (Sphere.Radius < 10f)
                Sphere.Radius = 10f;
            switch (type)
            {
                case GizmoType.X:
                    Sphere.Center.X += Sphere.Radius;
                    transformMatrix = Matrix.Scaling(Sphere.Radius / 2f) * Matrix.RotationY(MathUtil.Pi / 2) * Matrix.Translation(Sphere.Center);
                    break;
                case GizmoType.Y:
                    Sphere.Center.Y += Sphere.Radius;
                    transformMatrix = Matrix.Scaling(Sphere.Radius / 2f) * Matrix.RotationX(-MathUtil.Pi / 2) * Matrix.Translation(Sphere.Center);
                    break;
                case GizmoType.Z:
                    Sphere.Center.Z += Sphere.Radius;
                    transformMatrix = Matrix.Scaling(Sphere.Radius / 2f) * Matrix.Translation(Sphere.Center);
                    break;
            }

            boundingBox = BoundingBox.FromPoints(SharpRenderer.pyramidVertices.ToArray());
            boundingBox.Maximum = (Vector3)Vector3.Transform(boundingBox.Maximum, transformMatrix);
            boundingBox.Minimum = (Vector3)Vector3.Transform(boundingBox.Minimum, transformMatrix);
        }

        private DefaultRenderData renderData;

        public void Draw(SharpRenderer renderer)
        {
            renderData.worldViewProjection = transformMatrix * renderer.viewProjection;

            renderer.Device.SetFillModeSolid();
            renderer.Device.SetCullModeNone();
            renderer.Device.SetBlendStateAlphaBlend();
            renderer.Device.ApplyRasterState();
            renderer.Device.SetDepthStateNone();
            renderer.Device.UpdateAllStates();

            renderer.Device.UpdateData(renderer.basicBuffer, renderData);
            renderer.Device.DeviceContext.VertexShader.SetConstantBuffer(0, renderer.basicBuffer);
            renderer.basicShader.Apply();

            renderer.Pyramid.Draw(renderer.Device);

            renderer.Device.SetDefaultDepthState();
        }

        public BoundingBox boundingBox;

        public float? IntersectsWith(Ray r)
        {
            if (r.Intersects(ref boundingBox, out float distance))
                if (TriangleIntersection(r))
                    return distance;

            return null;
        }

        public bool TriangleIntersection(Ray r)
        {
            List<Vector3> pyramidVertices = SharpRenderer.pyramidVertices;

            foreach (LevelEditor.Triangle t in SharpRenderer.pyramidTriangles)
            {
                Vector3 v1 = (Vector3)Vector3.Transform(pyramidVertices[t.vertex1], transformMatrix);
                Vector3 v2 = (Vector3)Vector3.Transform(pyramidVertices[t.vertex2], transformMatrix);
                Vector3 v3 = (Vector3)Vector3.Transform(pyramidVertices[t.vertex3], transformMatrix);

                if (r.Intersects(ref v1, ref v2, ref v3))
                    return true;
            }
            return false;
        }
    }
}