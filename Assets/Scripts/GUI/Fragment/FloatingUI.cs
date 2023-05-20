#nullable enable
using System;
using UnityEngine;

using MinecraftClient.Mapping;

namespace MinecraftClient.UI
{
    public abstract class FloatingUI : MonoBehaviour
    {
        protected Entity? entity;

        public abstract void SetInfo(Entity entity);

        public virtual void Destroy(Action callback)
        {
            callback?.Invoke();
            Destroy(gameObject);
        }
    }
}