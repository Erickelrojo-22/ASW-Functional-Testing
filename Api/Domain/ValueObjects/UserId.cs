using Vogen;

namespace Api.Domain.ValueObjects;

[ValueObject(typeof(Guid))]
public partial struct UserId
{
    public static UserId New() => From(Guid.NewGuid());
}
