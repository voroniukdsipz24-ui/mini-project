namespace HotelBooking.Domain.Entities;

/// <summary>
/// Абстрактний базовий клас для всіх сутностей предметної області.
/// Демонструє: абстракція, інкапсуляція Id, поліморфний Display().
/// </summary>
public abstract class EntityBase
{
    public int Id { get; protected set; }

    /// <summary>
    /// Поліморфний метод відображення — кожна сутність реалізує по-своєму.
    /// Використовується в ConsoleUI замість прямого ToString().
    /// </summary>
    public abstract string Display();

    public override string ToString() => Display();

    public override bool Equals(object? obj) =>
        obj is EntityBase other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>
/// Абстрактний базовий клас для людей у системі (Guest може розширитись Staff тощо).
/// Демонструє ієрархію з спільною логікою валідації імені.
/// </summary>
public abstract class PersonBase : EntityBase
{
    public string FirstName { get; protected set; }
    public string LastName  { get; protected set; }
    public string Email     { get; protected set; }

    protected PersonBase(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("Valid email is required.", nameof(email));

        FirstName = firstName.Trim();
        LastName  = lastName.Trim();
        Email     = email.Trim().ToLowerInvariant();
    }

    // Needed for deserialization
    protected PersonBase()
    {
        FirstName = LastName = Email = string.Empty;
    }

    public string FullName => $"{FirstName} {LastName}";
}
