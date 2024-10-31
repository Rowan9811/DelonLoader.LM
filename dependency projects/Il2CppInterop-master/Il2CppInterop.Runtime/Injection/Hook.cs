﻿using System;
using System.Runtime.InteropServices;
using Il2CppInterop.Common;
using Microsoft.Extensions.Logging;

namespace Il2CppInterop.Runtime.Injection
{
    internal abstract class Hook<T> where T : Delegate
    {
        private bool _isApplied;
        private T _detour;
        private T _method;
        private T _original;

        public T Original => _original;

        public abstract string TargetMethodName { get; }
        public abstract T GetDetour();
        public abstract IntPtr FindTargetMethod();

        public virtual void TargetMethodNotFound()
        {
            Logger.Instance.LogWarning("Target hook method {TargetMethodName} not found", TargetMethodName);
            _isApplied = true; // lying because reattempting is useless
        }

        public void ApplyHook()
        {
            try
            {
                if (_isApplied) return;

                var methodPtr = FindTargetMethod();

                if (methodPtr == IntPtr.Zero)
                {
                    TargetMethodNotFound();
                    return;
                }

                Logger.Instance.LogTrace("{MethodName} found: 0x{MethodPtr}", TargetMethodName, methodPtr.ToInt64().ToString("X2"));

                _detour = GetDetour();
                Detour.Apply(methodPtr, _detour, out _original);
                _method = Marshal.GetDelegateForFunctionPointer<T>(methodPtr);
                _isApplied = true;
            }
            catch (Exception ex)
            {
                TargetMethodNotFound();
                Logger.Instance.LogError(ex.ToString());
            }
        }
    }
}
