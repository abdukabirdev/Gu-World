using UnityEngine;
using UnityEngine.Rendering;

namespace GuWorld.Fx
{
    /// <summary>
    /// The hitbox view of the developer's mode (F7; ROADMAP 2.11 — main's F7, improved): the colliders
    /// within 30 m drawn as wire boxes after the camera has drawn the world — bodies (actors) red,
    /// triggers yellow, the rest green; at most 300 of them.
    /// </summary>
    public static class Hitboxes
    {
        public static bool On { get; private set; }
        static Material line;
        static readonly Collider[] found = new Collider[300];

        public static void Toggle()
        {
            On = !On;

            if (On)
            {
                RenderPipelineManager.endCameraRendering += Draw;
            }
            else
            {
                RenderPipelineManager.endCameraRendering -= Draw;
            }
        }

        static void Draw(ScriptableRenderContext context, Camera camera)
        {
            if (Game.Hero == null || camera != Game.Rig?.Camera)
            {
                return;
            }

            if (line == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");

                if (shader == null)
                {
                    return;
                }

                line = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                line.SetInt("_ZTest", (int)CompareFunction.Always);
                line.SetInt("_Cull", (int)CullMode.Off);
            }

            var count = Physics.OverlapSphereNonAlloc(Game.Hero.transform.position, 30, found, ~0, QueryTriggerInteraction.Collide);
            line.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            GL.Begin(GL.LINES);

            for (var i = 0; i < count; i++)
            {
                var collider = found[i];

                if (collider == null || collider is TerrainCollider)
                {
                    continue;
                }

                GL.Color(collider.GetComponentInParent<Actors.Actor>() != null ? new Color(1, 0.35f, 0.3f) : collider.isTrigger ? new Color(1, 0.85f, 0.3f) : new Color(0.4f, 1, 0.6f));
                Box(collider.bounds);
            }

            GL.End();
            GL.PopMatrix();
        }

        static void Box(Bounds b)
        {
            var min = b.min;
            var max = b.max;
            var c = new[]
            {
                new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z),
            };

            for (var k = 0; k < 4; k++)
            {
                Line(c[k], c[(k + 1) % 4]);
                Line(c[k + 4], c[(k + 1) % 4 + 4]);
                Line(c[k], c[k + 4]);
            }
        }

        static void Line(Vector3 a, Vector3 b)
        {
            GL.Vertex(a);
            GL.Vertex(b);
        }
    }
}
