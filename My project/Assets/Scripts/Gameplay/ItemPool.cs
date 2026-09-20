using System.Collections.Generic;
using UnityEngine;

namespace Scrambly.Catch
{
    /// <summary>
    /// Pre-warmed pool of falling collectibles. Avoids Instantiate/Destroy during play.
    /// </summary>
    /// <remarks>
    /// <see cref="PlayableBootstrap"/> calls <see cref="Warm"/> once with <see cref="GameConfig.PoolSize"/>.
    /// If every instance is active, <see cref="Spawn"/> returns null and the spawner skips that beat
    /// rather than growing the zip/heap. Restart uses <see cref="DespawnAll"/> so leftover catch/miss
    /// animations cannot leak into the next round.
    /// Visual meshes are quads parented under each item; materials are swapped on Launch, not on Warm.
    /// </remarks>
    public sealed class ItemPool : MonoBehaviour
    {
        private readonly List<FallingItem> _all = new List<FallingItem>(24);
        private readonly Stack<FallingItem> _inactive = new Stack<FallingItem>(24);
        private readonly List<FallingItem> _active = new List<FallingItem>(24);

        /// <summary>
        /// Items currently falling or playing a catch/miss outro. The director iterates this list backwards.
        /// </summary>
        public List<FallingItem> Active => _active;

        /// <summary>
        /// Destroys any previous instances and builds <paramref name="count"/> sleeping items under this transform.
        /// </summary>
        /// <param name="count">Pool capacity from GameConfig. Extra spawn requests are dropped.</param>
        /// <param name="kit">Provides the shared quad mesh bound to each visual child.</param>
        public void Warm(int count, VisualKit kit)
        {
            Clear();
            for (int i = 0; i < count; i++)
            {
                FallingItem item = CreateInstance(kit, i);
                item.Sleep();
                _all.Add(item);
                _inactive.Push(item);
            }
        }

        /// <summary>
        /// Takes one inactive item and marks it active. Does not launch it; the spawner must call Launch.
        /// </summary>
        /// <returns>A sleeping item ready to Launch, or null when the pool is exhausted.</returns>
        public FallingItem Spawn()
        {
            if (_inactive.Count == 0)
            {
                return null;
            }

            FallingItem item = _inactive.Pop();
            _active.Add(item);
            return item;
        }

        /// <summary>
        /// Sleeps one item and returns it to the inactive stack. Safe if the item is already inactive.
        /// </summary>
        /// <param name="item">Instance from <see cref="Active"/> after catch/miss outro finishes.</param>
        public void Despawn(FallingItem item)
        {
            if (item == null)
            {
                return;
            }

            item.Sleep();
            _active.Remove(item);
            if (!_inactive.Contains(item))
            {
                _inactive.Push(item);
            }
        }

        /// <summary>
        /// Sleeps every active item without destroying them. Required for a clean Restart and round end.
        /// </summary>
        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                FallingItem item = _active[i];
                item.Sleep();
                _inactive.Push(item);
            }

            _active.Clear();
        }

        /// <summary>
        /// Destroys pooled GameObjects. Only used when re-warming (editor play / bootstrap).
        /// </summary>
        private void Clear()
        {
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i] != null)
                {
                    Destroy(_all[i].gameObject);
                }
            }

            _all.Clear();
            _inactive.Clear();
            _active.Clear();
        }

        /// <summary>
        /// Builds one pooled item: root + Visual quad with shadows/probes off.
        /// </summary>
        private FallingItem CreateInstance(VisualKit kit, int index)
        {
            var go = new GameObject("FallingItem_" + index);
            go.transform.SetParent(transform, false);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            var filter = visual.AddComponent<MeshFilter>();
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            filter.sharedMesh = kit.Quad;
            var item = go.AddComponent<FallingItem>();
            item.Bind(filter, renderer, visual.transform);
            return item;
        }
    }
}
