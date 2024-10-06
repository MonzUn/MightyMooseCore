using static Eco.Moose.Data.Enums;

namespace Eco.Moose.Features
{
    public class LookupResult
    {
        public LookupResult(LookupResultTypes result, IReadOnlyList<object> matches, string errorMessage)
        {
            Result = result;
            Matches = matches;
            ErrorMessage = errorMessage;
            MatchedTypes = LookupTypes.None;
            foreach (object entity in matches)
            {
                MatchedTypes |= DynamicLookup.GetEntityType(entity);
            }
        }

        public LookupResultTypes Result { get; private set; }
        public IReadOnlyList<object> Matches { get; private set; }
        public LookupTypes MatchedTypes { get; private set; }
        public string ErrorMessage { get; private set; }
    }
}
