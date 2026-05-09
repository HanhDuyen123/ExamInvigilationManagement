namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class RoleMapping
    {
        public static Domain.Entities.Role ToDomain(this Data.Entities.Role entity)
        {
            return new Domain.Entities.Role
            {
                Id = entity.RoleId,
                Name = entity.RoleName
            };
        }

        public static Data.Entities.Role ToEntity(this Domain.Entities.Role domain)
        {
            return new Data.Entities.Role
            {
                //RoleId = domain.Id,
                RoleName = domain.Name
            };
        }
    }
}
