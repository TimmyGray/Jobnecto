public class UserRepository : EditableRepository<User>
{
    public UserRepository(AppDbContext context)
        : base(context) { }
}
