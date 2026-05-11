namespace HotelBooking.Domain.Entities;

/// <summary>
/// Гість готелю. Наслідує PersonBase — отримує спільну валідацію імені/email
/// та поліморфний Display().
/// </summary>
public class Guest : PersonBase
{
    public string Phone          { get; private set; }
    public string PassportNumber { get; private set; }
    public DateTime DateOfBirth  { get; private set; }

    // For JSON deserialization
    private Guest() { Phone = PassportNumber = string.Empty; }

    public Guest(int id, string firstName, string lastName, string email,
                 string phone, string passportNumber, DateTime dateOfBirth)
        : base(firstName, lastName, email)
    {
        Id             = id;
        Phone          = phone.Trim();
        PassportNumber = passportNumber.Trim();
        DateOfBirth    = dateOfBirth;
    }

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            int age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>Поліморфна реалізація Display() з EntityBase.</summary>
    public override string Display() =>
        $"Guest #{Id}: {FullName} | {Email} | {Phone}";
}
