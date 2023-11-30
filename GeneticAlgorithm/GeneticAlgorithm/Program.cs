using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GeneticScheduler
{
    public class Class
    {
        public string Subject { get; set; }
        public string Teacher { get; set; }
        public string Group { get; set; }
        public int Time { get; set; }
        public string Audience { get; set; }
    }

    public class GeneticScheduler
    {
        private readonly List<string> _subjects;
        private readonly List<string> _teachers;
        private readonly List<string> _groups;
        private readonly List<string> _audiences;
        private readonly int _classesPerDay;
        private readonly Dictionary<string, List<string>> _teacherSubjects;
        private readonly Dictionary<string, int> _teacherMaxHours;
        private readonly Dictionary<string, List<string>> _groupsSubjects;
        private static readonly Random Random = new Random();



        public GeneticScheduler(List<string> subjects, List<string> teachers, List<string> groups, int classesPerDay, Dictionary<string, List<string>> teacherSubjects, List<string> audiences, Dictionary<string, int> teacherMaxHours, Dictionary<string, List<string>> groupsSubjects)
        {
            _subjects = subjects;
            _teachers = teachers;
            _groups = groups;
            _classesPerDay = classesPerDay;
            _teacherSubjects = teacherSubjects;
            _audiences = audiences;
            _teacherMaxHours = teacherMaxHours;
            _groupsSubjects = groupsSubjects;
        }

        private List<Class> GenerateRandomSchedule()
        {
            return _subjects.Select(subject => new Class
            {
                Subject = subject,
                Teacher = _teachers[Random.Next(_teachers.Count)],
                Group = _groups[Random.Next(_groups.Count)],
                Time = Random.Next(1, _classesPerDay + 1),
                Audience = _audiences[Random.Next(_audiences.Count)]
            }).ToList();
        }

        private List<List<Class>> GenerateRandomPopulation(int populationSize)
        {
            return Enumerable.Range(0, populationSize).Select(_ => GenerateRandomSchedule()).ToList();
        }

        private static double CalculateFitness(List<Class> schedule, Dictionary<string, List<string>> teacherSubjects, Dictionary<string, int> teacherMaxHours, Dictionary<string, List<string>> groupsSubjects)
        {
            int conflicts = schedule
                .SelectMany((c, i) => schedule.Skip(i + 1), (c1, c2) => new { c1, c2 })
                .Count(pair => pair.c1.Time == pair.c2.Time && pair.c1.Group == pair.c2.Group ||
                               pair.c1.Teacher == pair.c2.Teacher && pair.c1.Time == pair.c2.Time ||
                               pair.c1.Time == pair.c2.Time && pair.c1.Audience == pair.c2.Audience);

            // Додавання конфліктів, якщо викладач читає невідповідний предмет
            conflicts += schedule.Count(c => !teacherSubjects[c.Teacher].Contains(c.Subject));
            conflicts += schedule.Count(c => !groupsSubjects[c.Group].Contains(c.Subject));

            // Перевірка кількості годин викладання на перевищення максимальної кількості для кожного викладача
            var teachingHours = schedule.GroupBy(c => c.Teacher)
                                       .ToDictionary(group => group.Key, group => group.Sum(c => c.Time));
            foreach (var teacher in teachingHours)
            {
                if (teacherMaxHours.ContainsKey(teacher.Key) && teacher.Value > teacherMaxHours[teacher.Key])
                {
                    conflicts++; // Додаємо конфлікт, якщо викладач перевищує максимальну кількість годин
                }
            }

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
                    Time = Random.Next(1, _classesPerDay + 1),
                    Audience = _audiences[Random.Next(_audiences.Count)],
                }
                : c).ToList();
        }

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


        //    public (List<Class>, double) Solve(int populationSize, int generations)
        //    {
        //        var population = GenerateRandomPopulation(populationSize);
        //        double bestFitnessScore = 0;
        //        List<Class> bestSchedule = null;

        //        for (int generation = 0; generation < generations; generation++)
        //        {
        //            var fitnessScores = population.Select(schedule => CalculateFitness(schedule, _teacherSubjects, _teacherMaxHours, _groupsSubjects)).ToList();

        //            int bestIndex = fitnessScores.IndexOf(fitnessScores.Max());
        //            bestFitnessScore = fitnessScores[bestIndex];
        //            bestSchedule = population[bestIndex];

        //            Console.WriteLine($"Generation {generation + 1}: Best rating = {bestFitnessScore}");
        //            Console.WriteLine();


        //            var newPopulation = new List<List<Class>>();
        //            while (newPopulation.Count < populationSize)
        //            {
        //                var parent1 = population[Random.Next(population.Count)];
        //                var parent2 = population[Random.Next(population.Count)];
        //                var (child1, child2) = Crossover(parent1, parent2);
        //                newPopulation.Add(Mutate(child1));
        //                newPopulation.Add(Mutate(child2));
        //            }

        //            population = newPopulation.Take(populationSize).ToList();
        //        }

        //        return (bestSchedule, bestFitnessScore);
        //    }
        //}

        public (List<Class>, double) Solve(int populationSize, int generations)
        {
            var population = GenerateRandomPopulation(populationSize);
            double bestFitnessScore = 0;
            List<Class> bestSchedule = null;

            // Визначення ймовірностей
            double crossoverProbability = 0.8; // Ймовірність кросоверу 80%
            double mutationProbability = 0.1; // Ймовірність мутації гену 10%
            double mutationGeneralProbability = 0.8; // Ймовірність мутації гену 10%


            for (int generation = 0; generation < generations; generation++)
            {
                var fitnessScores = population.Select(schedule => CalculateFitness(schedule, _teacherSubjects, _teacherMaxHours, _groupsSubjects)).ToList();

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

                    // Застосування кросоверу на основі ймовірності
                    if (Random.NextDouble() < crossoverProbability)
                    {
                        var (child1, child2) = Crossover(parent1, parent2);
                        newPopulation.Add(child1);
                        newPopulation.Add(child2);
                    }
                    else
                    {
                        // Якщо кросовер не відбувається, батьків додаємо безпосередньо
                        newPopulation.Add(parent1);
                        newPopulation.Add(parent2);
                    }

                    if (Random.NextDouble() < mutationGeneralProbability)
                    {
                        // Застосування мутації на основі ймовірності
                        for (int i = newPopulation.Count - 2; i < newPopulation.Count; i++)
                        {
                            if (Random.NextDouble() < mutationProbability)
                            {
                                newPopulation[i] = Mutate(newPopulation[i]);
                            }
                        }
                    }
                }

                population = newPopulation.Take(populationSize).ToList();
            }

            return (bestSchedule, bestFitnessScore);
        }


        class Program
        {
            static void Main(string[] args)
            {
                var subjects = new List<string> { "Математичний аналiз", "Програмування", "Ядерна фiзика", "Алгебра та геометрiя", "Механiка", "Управлiння проектами" };
                var teachers = new List<string> { "Миколенко", "Зiнченко", "Мудрик", "Забарний", "Довбик", "Циганков" };
                var groups = new List<string> { "МАТ-21", "ФIЗ-32", "МАТ-22", "ФIЗ-31", "ПРОГ-41", "ПРОГ-42" };
                var teacherSubjects = new Dictionary<string, List<string>>
            {
                { "Миколенко", new List<string> { "Математичний аналiз", "Алгебра та геометрiя" } },
                { "Зiнченко", new List<string> { "Програмування", "Управлiння проектами" } },
                { "Мудрик", new List<string> { "Ядерна фiзика" } },
                { "Забарний", new List<string> { "Механiка" } },
                { "Довбик", new List<string> { "Програмування" } },
                { "Циганков", new List<string> { "Управлiння проектами", "Алгебра та геометрiя" } }
            };

                var groupsSubjects = new Dictionary<string, List<string>>
            {
                { "МАТ-21", new List<string> { "Математичний аналiз", "Алгебра та геометрiя" } },
                { "МАТ-22", new List<string> { "Математичний аналiз", "Алгебра та геометрiя" } },
                { "ПРОГ-41", new List<string> { "Програмування", "Управлiння проектами" } },
                { "ФIЗ-31", new List<string> { "Ядерна фiзика" } },
                { "ФIЗ-32", new List<string> { "Механiка" } },
                { "ПРОГ-42", new List<string> { "Програмування" } }
            };

                var teacherMaxHours = new Dictionary<string, int>
                {
                    { "Миколенко", 20 },
                    { "Зiнченко", 30 },
                    { "Мудрик", 20 },
                    { "Забарний", 10 },
                    { "Довбик", 30 },
                    { "Циганков", 20 }
                };

                var audiences = new List<string> { "01", "02", "03", "04", "05", "06" };

                int classesPerDay = 5;

                var scheduler = new GeneticScheduler(subjects, teachers, groups, classesPerDay, teacherSubjects, audiences, teacherMaxHours, groupsSubjects);
                var (bestSchedule, fitness) = scheduler.Solve(500, 100);

                Console.WriteLine("Best schedule:");
                foreach (var lesson in bestSchedule)
                {
                    Console.WriteLine($"{lesson.Subject} - {lesson.Teacher} - {lesson.Group} - {lesson.Time} - {lesson.Audience}");
                }
                Console.WriteLine($"Rating: {fitness}");
            }
        }
    }
}
