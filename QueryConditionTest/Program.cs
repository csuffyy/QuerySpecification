using System;
using System.Collections.Generic;
using System.Linq;
using QuerySpecification;

namespace QueryConditionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var userService = new UserService();

            var specification = new Specification<User>
            {
                // 导航属性
                IncludedNavigationProperties = new List<string> { "FisrtName", "LastName" },

                // 分页参数
                Pagination = new Pagination(2, 1)
            };

            // 查询条件
            var firstCriteria = Criteria<User>.True();
            firstCriteria = firstCriteria.And(user => user.FirstName, Operator.Like, "1");

            var secondCriteria = Criteria<User>.True();
            secondCriteria = secondCriteria.Or(user => user.LastName, Operator.Like, "2");
            //secondCriteria = secondCriteria.Or(user => user.LastName, Operator.EndsWith, "3");

            var finalCriteria = firstCriteria.And(secondCriteria);

            // 查询条件
            specification.Criteria = finalCriteria;

            // 排序条件
            var sortCondition = SortCondition<User>.OrderBy(q => q.FirstName).ThenByDescending(q => q.LastName);
            specification.SortCondition = sortCondition;

            string fileName = "Spec.json";

            specification.Save(fileName);
            var spec = Specification<User>.LoadFromFile(fileName);

            var users = userService.GetUser(spec);

            Console.WriteLine("Result:");

            foreach (var user in users)
            {
                Console.WriteLine(user);
            }

            Console.ReadLine();
        }
    }

    internal class UserService
    {
        private readonly IEnumerable<User> context = Enumerable.Range(1, 100)
            .Select(x => new User()
            {
                FirstName = "FirstName" + x,
                LastName = "LastName" + (100 - x),
                Comments = new[] { "Comments" + x }
            });

        public List<User> GetUser(Specification<User> spec)
        {
            //方法1：
            var users = context.AsQueryable().Query(spec).ToList();
            return users;

            //方法2：
            //var users = context.Query(spec).ToList();
            //return users;
        }
    }

    internal class User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ICollection<string> Comments { get; set; }

        public override string ToString()
        {
            return $"{FirstName}.{LastName}";
        }
    }
}
