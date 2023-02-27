using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    public class NegateChannel : ValueChannel
    {
        private ValueChannel target;

        public NegateChannel(ValueChannel channel)
        {
            target = channel;
        }

        public override void DeserializeProperties(List<object> properties, ScenecontrolDeserialization deserialization)
        {
            target = deserialization.GetUnitFromId((int)properties[0]) as ValueChannel;
        }

        public override List<object> SerializeProperties(ScenecontrolSerialization serialization)
        {
            return new List<object>
            {
                serialization.AddUnitAndGetId(target),
            };
        }

        public override float ValueAt(int timing)
        {
            return -target.ValueAt(timing);
        }
    }
}