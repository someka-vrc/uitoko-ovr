using R3;
using System;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Serialization;

internal class ReactiveOnlyValueResolver : DefaultContractResolver
{
    protected override JsonContract CreateContract(Type objectType)
    {
        // SerializableReactiveProperty<T> だけに対して特殊な挙動
        if (objectType.IsGenericType &&
            objectType.GetGenericTypeDefinition() == typeof(SerializableReactiveProperty<>))
        {
            // 強制的に "Value" プロパティだけを扱うようにする
            var contract = base.CreateContract(objectType);
            contract.Converter = (JsonConverter)Activator.CreateInstance(
                typeof(ReactivePropertyConverter<>).MakeGenericType(objectType.GenericTypeArguments[0])
            );
            return contract;
        }

        return base.CreateContract(objectType);
    }
}
