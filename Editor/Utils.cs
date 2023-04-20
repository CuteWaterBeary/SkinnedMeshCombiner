/**
 * The MIT License (MIT)
 *
 * Copyright (c) 2023 Jeremy Lam aka. Vistanz
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace JLChnToZ.EditorExtensions.SkinnedMeshCombiner {

    [Flags]
    public enum BlendShapeCopyMode : byte {
        None = 0,
        Vertices = 0x1,
        Normals = 0x2,
        Tangents = 0x4,
    }

    public static class Utils {

        public static bool LazyInitialize<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, out TValue value) where TValue : new() {
            if (!dict.TryGetValue(key, out value)) {
                dict[key] = value = new TValue();
                return true;
            }
            return false;
        }

        public static bool LazyInitialize<T>(ref T value) where T : new() {
            if (value == null) {
                value = new T();
                return true;
            }
            return false;
        }

        public static void PreAllocate<T>(List<T> list, int capacity) {
            capacity += list.Count;
            if (list.Capacity < capacity) list.Capacity = capacity;
        }
        
        public static (Vector3[], Vector3[], Vector3[]) GetVNTArrays(
            ref Dictionary<int, (Vector3[], Vector3[], Vector3[])> vntArrayCache,
            int vertexCount, BlendShapeCopyMode copyMode
        ) {
            LazyInitialize(ref vntArrayCache);
            if (!vntArrayCache.TryGetValue(vertexCount, out var vntArray))
                vntArrayCache[vertexCount] = vntArray = (
                    copyMode.HasFlag(BlendShapeCopyMode.Vertices) ? new Vector3[vertexCount] : null,
                    copyMode.HasFlag(BlendShapeCopyMode.Normals) ? new Vector3[vertexCount] : null,
                    copyMode.HasFlag(BlendShapeCopyMode.Tangents) ? new Vector3[vertexCount] : null
                );
            return vntArray;
        }

        public static void LerpVNTArray(
            Vector3[] prev, int prevIndex, Vector3[] next, int nextIndex, Vector3[] dest, int destIndex,
            float weight
        ) {
            if (prev != null && next != null && dest != null)
                dest[destIndex] = Vector3.Lerp(prev[prevIndex], next[nextIndex], weight);
        }

        public static void CopyVNTArrays(
            (Vector3[], Vector3[], Vector3[], float) frameData, (SubMeshDescriptor, int, int, Matrix4x4?) subMeshData, 
            Vector3[] destDeltaVertices, Vector3[] destDeltaNormals, Vector3[] destDeltaTangents
        ) {
            var (deltaVertices, deltaNormals, deltaTangents, _) = frameData;
            var (subMesh, _, destOffset, transform) = subMeshData;
            var srcOffset = subMesh.firstVertex;
            var srcVertexCount = subMesh.vertexCount;
            if (deltaVertices != null && destDeltaVertices != null) Array.Copy(deltaVertices, srcOffset, destDeltaVertices, destOffset, srcVertexCount);
            if (deltaNormals != null && destDeltaNormals != null) Array.Copy(deltaNormals, srcOffset, destDeltaNormals, destOffset, srcVertexCount);
            if (deltaTangents != null && destDeltaTangents != null) Array.Copy(deltaTangents, srcOffset, destDeltaTangents, destOffset, srcVertexCount);
            if (transform.HasValue) {
                if (deltaVertices != null && destDeltaVertices != null) TransformEach(destDeltaVertices, destOffset, srcVertexCount, transform.Value);
                if (deltaNormals != null && destDeltaNormals != null) TransformEach(destDeltaNormals, destOffset, srcVertexCount, transform.Value);
                if (deltaTangents != null && destDeltaTangents != null) TransformEach(destDeltaTangents, destOffset, srcVertexCount, transform.Value);
            }
        }

        public static Transform FindCommonParent(IEnumerable<Transform> transforms) {
            Transform commonParent = null;
            foreach (var transform in transforms) {
                if (transform == null) continue;
                if (commonParent == null) {
                    commonParent = transform;
                    continue;
                }
                var parent = transform;
                while (parent != null) {
                    if (parent == commonParent || parent.IsChildOf(commonParent))
                        break;
                    if (commonParent.IsChildOf(parent)) {
                        commonParent = parent;
                        break;
                    }
                    parent = parent.parent;
                }
            }
            return commonParent;
        }

        public static void TransformEach(Vector3[] deltas, int offset, int count, Matrix4x4 transform) {
            for (var i = 0; i < count; i++)
                deltas[offset + i] = transform.MultiplyVector(deltas[offset + i]);
        }
    }
}