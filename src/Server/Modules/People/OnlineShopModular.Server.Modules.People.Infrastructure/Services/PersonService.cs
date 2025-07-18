
using OnlineShopModular.Server.Modules.People.Domain.Entities;
using OnlineShopModular.Server.Modules.People.Infrastructure.Persistence;

namespace OnlineShopModular.Server.Modules.People.Infrastructure.Services;
public class PersonService
{
    private readonly PersonDbContext _context;

    public PersonService(PersonDbContext context)
    {
        _context = context;
    }

    public async Task<PersonResponseDto> CreateAsync(PersonRequestDto dto, CancellationToken cancellationToken)
    {
        var person = new Person { FullName = dto.FullName };
        _context.People.AddAsync(person);
        await _context.SaveChangesAsync(cancellationToken);
        return new PersonResponseDto { Id = person.Id, FullName = person.FullName };
    }

    public async Task<PersonResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var person = await _context.People.FindAsync(new object[] { id }, cancellationToken);

        return person == null
            ? null
            : new PersonResponseDto
            {
                Id = person.Id,
                FullName = person.FullName
            };
    }

}
