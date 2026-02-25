using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using Reflex.Attributes;

public class Entity : MonoBehaviour {
    [ReadOnly] public string        AssetAddress;
               public EntityFlags   Flags;
               public EntityType    Type;
    [DontSave] public EntityHandle  Handle;
    [DontSave] public bool          AutoBake;
    [DontSave] public EntityManager Em;

    public Vector3 Position {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => transform.position = value;
    }

    public Quaternion Rotation {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.rotation;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => transform.rotation = value;
    }

    public Vector3 Scale {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.localScale;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => transform.localScale = value;
    }

    public Matrix4x4 ObjectToWorld {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => transform.localToWorldMatrix;
    }

    [Inject]
    protected virtual void Inject(EntityManager em) {
        if (AutoBake) {
            em.BakeEntity(this);
        }
    }

    public virtual void OnBaking(){ }
    public virtual void OnCreate(){ }
    public virtual void UpdateEntity(){ }
    public virtual void Destroy() { }
    public virtual void OnBecameDynamic() { }
    public virtual void OnBecameStatic() { }

    // Helper methods to get and destroy entities without calling Em...
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntity(EntityHandle handle, out Entity e) {
        return Em.GetEntity(handle, out e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool GetEntity<T>(EntityHandle handle, out T e)
    where T : Entity {
        return Em.GetEntity<T>(handle, out e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyThisEntity() {
        Em.DestroyEntity(Handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(EntityHandle handle) {
        Em.DestroyEntity(handle);
    }
}