using System;
using System.Collections.Generic;
using System.Linq;

namespace GeneticScheduler
{
    public class Class
    {
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public string Group { get; set; }
        public int Time { get; set; }
    }

    public class GeneticScheduler
    {
        private readonly List<string> _subjects;
        private readonly List<string> _teachers;
        private readonly List<string> _groups;
        private readonly int _classesPerDay;
        private static readonly Random Random = new Random();

        public GeneticScheduler(List<string> subjects, List<string> teachers, List<string> groups, int classesPerDay)
        {
            _subjects = subjects;
            _teachers = teachers;
            _groups = groups;
            _classesPerDay = classesPerDay;
        }

        private List<Class> GenerateRandomSchedule()
        {
            return _subjects.Select(subject => new Class
            {
                Subject = subject,
                Teacher = _teachers[Random.Next(_teachers.Count)],
                Group = _groups[Random.Next(_groups.Count)],
                Time = Random.Next(1, _classesPerDay + 1)
            }).ToList();
        }

        private List<List<Class>> GenerateRandomPopulation(int populationSize)
        {
            return Enumerable.Range(0, populationSize).Select(_ => GenerateRandomSchedule()).ToList();
        }

        private static double CalculateFitness(List<Class> schedule)
        {
            int conflicts = schedule.SelectMany((c, i) => schedule.Skip(i + 1), (c1, c2) => new { c1, c2 })
                .Count(pair => pair.c1.Time == pair.c2.Time || pair.c1.Teacher == pair.c2.Teacher || pair.c1.Group == pair.c2.Group);

            Console.WriteLine($"Conflicts: {conflicts}, rating: {1.0 / (1.0 + conflicts)}");
            return 1.0 / (1.0 + conflicts);
        }

        private List<Class> Mutate(List<Class> schedule)
        {
            return schedule.Select(c => Random.NextDouble() < 0.1
                ? new Class
                {
                    Subject = c.Subject,
                    Teacher = _teachers[Random.Next(_teachers.Count)],
                    Group = _groups[Random.Next(_groups.Count)],
                    Time = Random.Next(1, _classesPerDay + 1)
                }
                : c).ToList();
        }

        //private (List<Class>, List<Class>) Crossover(List<Class> schedule1, List<Class> schedule2)
        //{
        //    int crossoverPoint = Random.Next(1, _subjects.Count);
        //    var child1 = schedule1.Take(crossoverPoint).Concat(schedule2.Skip(crossoverPoint)).ToList();
        //    var child2 = schedule2.Take(crossoverPoint).Concat(schedule1.Skip(crossoverPoint)).ToList();
        //    return (child1, child2);
        //}
        
        private (List<Class>, List<Class>) Crossover(List<Class> schedule1, List<Class> schedule2)
        {
            // Визначення двох випадкових точок для кросоверу
            int crossoverPoint1 = Random.Next(1, _subjects.Count - 1);
            int crossoverPoint2 = Random.Next(crossoverPoint1 + 1, _subjects.Count);

            // Створення нових дитячих розкладів з використанням сегментів між двома точками перехрещування
            var child1 = schedule1.Take(crossoverPoint1)
                                  .Concat(schedule2.Skip(crossoverPoint1).Take(crossoverPoint2 - crossoverPoint1))
                                  .Concat(schedule1.Skip(crossoverPoint2))
                                  .ToList();
            var child2 = schedule2.Take(crossoverPoint1)
                                  .Concat(schedule1.Skip(crossoverPoint1).Take(crossoverPoint2 - crossoverPoint1))
                                  .Concat(schedule2.Skip(crossoverPoint2))
                                  .ToList();

            return (child1, child2);
        }


        private static List<Class> SelectBest(List<List<Class>> population, List<double> fitnessScores)
        {
            int bestIndex = fitnessScores.IndexOf(fitnessScores.Max());
            return population[bestIndex];
        }

        public (List<Class>, double) Solve(int populationSize, int generations)
        {
            var population = GenerateRandomPopulation(populationSize);
            double bestFitnessScore = 0;
            List<Class> bestSchedule = null;

            for (int generation = 0; generation < generations; generation++)
            {
                var fitnessScores = population.Select(CalculateFitness).ToList();
                int bestIndex = fitnessScores.IndexOf(fitnessScores.Max());
                bestFitnessScore = fitnessScores[bestIndex];
                bestSchedule = population[bestIndex];

                Console.WriteLine($"Generation {generation + 1}: Best rating = {bestFitnessScore}");
                Console.WriteLine();


                var newPopulation = new List<List<Class>>();
                while (newPopulation.Count < populationSize)
                {
                    var parent1 = population[Random.Next(population.Count)];
                    var parent2 = population[Random.Next(population.Count)];
                    var (child1, child2) = Crossover(parent1, parent2);
                    newPopulation.Add(Mutate(child1));
                    newPopulation.Add(Mutate(child2));
                }

                population = newPopulation.Take(populationSize).ToList();
            }

            return (bestSchedule, bestFitnessScore);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var subjects = new List<string> { "Математичний аналiз", "Програмування", "Ядерна фiзика", "Алгебра та геометрiя", "Механiка", "Управлiння проектами" };
            var teachers = new List<string> { "Миколенко", "Зiнченко", "Мудрик", "Забарний", "Довбик", "Циганков" };
            var groups = new List<string> { "МАТ-21", "ФIЗ-32", "МАТ-22", "ФIЗ-31", "ПРОГ-41", "ПРОГ-42" };
            int classesPerDay = 5;

            var scheduler = new GeneticScheduler(subjects, teachers, groups, classesPerDay);
            var (bestSchedule, fitness) = scheduler.Solve(50, 10);

            Console.WriteLine("Best schedule:");
            foreach (var lesson in bestSchedule)
            {
                Console.WriteLine($"{lesson.Subject} - {lesson.Teacher} - {lesson.Group} - {lesson.Time}");
            }
            Console.WriteLine($"Rating: {fitness}");
        }
    }
}
