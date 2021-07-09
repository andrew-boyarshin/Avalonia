using System;
using System.Collections.Generic;

#pragma warning disable CS1591 // Enable me later

namespace Avalonia
{
    public class AvaloniaLocator : IAvaloniaDependencyResolver
    {
        private readonly IAvaloniaDependencyResolver _parentScope;
        public static IAvaloniaDependencyResolver Current { get; set; }
        public static AvaloniaLocator CurrentMutable { get; set; }
        private readonly Dictionary<Type, Func<object>> _registry = new Dictionary<Type, Func<object>>();

        static AvaloniaLocator()
        {
            Current = CurrentMutable = new AvaloniaLocator();
        }

        public AvaloniaLocator()
        {
            
        }

        public AvaloniaLocator(IAvaloniaDependencyResolver parentScope)
        {
            _parentScope = parentScope;
        }

        public object GetService(Type t)
        {
            Func<object> rv;
            return _registry.TryGetValue(t, out rv) ? rv() : _parentScope?.GetService(t);
        }

        private AvaloniaLocator AddOrUpdate<TService>(Func<object> creator)
        {
            _registry[typeof(TService)] = creator;
            return this;
        }

        private AvaloniaLocator Add<TService>(Func<object> creator, bool shouldIgnoreWhenBound)
        {
            var type = typeof(TService);

            if (!shouldIgnoreWhenBound)
            {
                _registry[type] = creator;
            }
            else if (GetService(type) is null)
            {
                try
                {
                    _registry.Add(type, creator);
                }
                catch (ArgumentException)
                {
                    // thread race lost
                }
            }

            return this;
        }

        public readonly ref struct RegistrationHelper<TService>
        {
            private readonly AvaloniaLocator _locator;
            private readonly bool _shouldIgnoreWhenBound;

            internal RegistrationHelper(AvaloniaLocator locator, bool shouldIgnoreWhenBound)
            {
                _locator = locator;
                _shouldIgnoreWhenBound = shouldIgnoreWhenBound;
            }

            public AvaloniaLocator ToConstant<TImpl>(TImpl constant) where TImpl : TService =>
                _locator.Add<TService>(() => constant, _shouldIgnoreWhenBound);

            public AvaloniaLocator ToFunc<TImpl>(Func<TImpl> func) where TImpl : TService =>
                _locator.Add<TService>(() => func(), _shouldIgnoreWhenBound);

            public AvaloniaLocator ToLazy<TImpl>(Func<TImpl> func) where TImpl : TService
            {
                var constructed = false;
                TImpl instance = default;
                return _locator.Add<TService>(
                    () =>
                    {
                        if (!constructed)
                        {
                            instance = func();
                            constructed = true;
                        }

                        return instance;
                    },
                    _shouldIgnoreWhenBound
                );
            }

            public AvaloniaLocator ToSingleton<TImpl>() where TImpl : class, TService, new()
            {
                TImpl instance = null;
                return _locator.Add<TService>(() => instance ??= new TImpl(), _shouldIgnoreWhenBound);
            }

            public AvaloniaLocator ToTransient<TImpl>() where TImpl : class, TService, new() =>
                _locator.Add<TService>(static() => new TImpl(), _shouldIgnoreWhenBound);
        }

        public RegistrationHelper<T> Bind<T>() => new(this, false);
        public RegistrationHelper<T> BindDefault<T>() => new(this, true);

        public AvaloniaLocator BindToSelf<T>(T constant) => AddOrUpdate<T>(() => constant);

        public AvaloniaLocator BindToSelfSingleton<T>() where T : class, new()
        {
            T instance = null;
            return AddOrUpdate<T>(() => instance ??= new T());
        }

        class ResolverDisposable : IDisposable
        {
            private readonly IAvaloniaDependencyResolver _resolver;
            private readonly AvaloniaLocator _mutable;

            public ResolverDisposable(IAvaloniaDependencyResolver resolver, AvaloniaLocator mutable)
            {
                _resolver = resolver;
                _mutable = mutable;
            }

            public void Dispose()
            {
                Current = _resolver;
                CurrentMutable = _mutable;
            }
        }


        public static IDisposable EnterScope()
        {
            var d = new ResolverDisposable(Current, CurrentMutable);
            Current = CurrentMutable =  new AvaloniaLocator(Current);
            return d;
        }
    }

    public interface IAvaloniaDependencyResolver
    {
        object GetService(Type t);
    }

    public static class LocatorExtensions
    {
        public static T GetService<T>(this IAvaloniaDependencyResolver resolver)
        {
            return (T) resolver.GetService(typeof (T));
        }
    }
}

