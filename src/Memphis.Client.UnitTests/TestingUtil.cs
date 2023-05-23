using System.Reflection;

namespace Memphis.Client.UnitTests
{
    public class TestingUtil
    {
        public static void setFieldToValue(object validatorObj, string fieldName, object val)
        {
            var prop = validatorObj.GetType().GetField(fieldName, BindingFlags.NonPublic
                                                                  | BindingFlags.Instance);
            #pragma warning disable CS8602 // Possible null reference argument.
            prop.SetValue(validatorObj, val);
        }
    }
}