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
                // navigation properties you wanna add  to entity
                IncludedNavigationProperties = new List<string> { "FisrtName", "LastName" },

                // data of pagination result, you can leat it to null if you don&#39;t have pagination	
                PagerArgs = new PagerArgs
                {
                    PageNumber = 1, // destination page number
                    ItemsPerPage = 2 // items per each pages
                }
            };

            // Filtering 
            var firstCriteria = Criteria<User>.True();
            firstCriteria = firstCriteria.And(user => user.FirstName, Operator.Like, "1");

            var secondCriteria = Criteria<User>.False();
            secondCriteria = secondCriteria.Or(user => user.LastName, Operator.Like, "2");
            secondCriteria = secondCriteria.Or(user => user.LastName, Operator.EndsWith, "3");

            var finalCriteria = firstCriteria.And(secondCriteria);

            specification.Criteria = finalCriteria;

            // Sorting
            var sortCondition = SortCondition<User>.OrderBy(q => q.FirstName).ThenByDescending(q => q.LastName);
            specification.SortCondition = sortCondition;

            string fileName = "Spec.json";

            specification.Save(fileName);
            var spec = Specification<User>.LoadFromFile(fileName);

            List<User> users = userService.GetUser(spec);
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

        public List<User> GetUser(Specification<User> queryCondition)
        {
            Func<IEnumerable<User>, IOrderedEnumerable<User>> enumerableSortingFunc = queryCondition.SortCondition.GetIEnumerableSortingExpression().Compile();
            var predicate = queryCondition.Criteria.GetExpression();
            var list = enumerableSortingFunc(context.AsQueryable().Where(predicate.Compile())).ToList();
            return list;
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
