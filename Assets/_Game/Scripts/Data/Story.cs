namespace ValourGames.Shawarma
{
    public class StoryLine
    {
        public int who;
        public string ru, en;
        public string Text { get { return L.T(ru, en); } }
        public StoryLine(int who, string ru, string en) { this.who = who; this.ru = ru; this.en = en; }
    }

    /// <summary>
    /// Сюжет: дядя Ашот передал тебе свой ларёк. Корпорация «МегаШаурма» забрала его ресторан.
    /// Пройди путь к миллиарду, переезжая всё выше, — и верни имя семьи.
    /// Главы: 0 — начало, 1 — первая встреча с конкурентом, 2..12 — переезд на локацию 1..11, 13 — финал.
    /// </summary>
    public static class Story
    {
        public const int Ashot = 0, King = 1, Reporter = 2, Me = 3;
        public const int Chapters = 14;

        public static string Who(int w)
        {
            switch (w)
            {
                case Ashot: return L.T("Дядя Ашот", "Uncle Ashot");
                case King: return L.T("Мистер Мега", "Mr. Mega");
                case Reporter: return L.T("Репортёр Лена", "Reporter Lena");
                default: return L.T("Ты", "You");
            }
        }

        static StoryLine A(string ru, string en) { return new StoryLine(Ashot, ru, en); }
        static StoryLine K(string ru, string en) { return new StoryLine(King, ru, en); }
        static StoryLine R(string ru, string en) { return new StoryLine(Reporter, ru, en); }
        static StoryLine M(string ru, string en) { return new StoryLine(Me, ru, en); }

        public static StoryLine[] Chapter(int id)
        {
            switch (id)
            {
                case 0: return new[]
                {
                    A("Сынок, этот ларёк кормил весь двор тридцать лет. Теперь он твой.", "Son, this kiosk fed the whole yard for thirty years. Now it's yours."),
                    A("Корпорация «МегаШаурма» скупает все точки в городе. Они забрали и мой ресторан.", "The “MegaShawarma” corporation is buying every spot in town. They took my restaurant too."),
                    A("Докажи, что настоящая шаурма сильнее. Пройди путь к миллиарду — и верни наше имя!", "Prove that real shawarma is stronger. Walk the road to a billion — and win our name back!"),
                    M("Я не подведу, дядя Ашот!", "I won't let you down, Uncle Ashot!"),
                };
                case 1: return new[]
                {
                    K("Милый ларёчек. Жаль, что скоро его не станет. Ха-ха!", "Cute little kiosk. Shame it won't last long. Ha-ha!"),
                    A("Не слушай его. Работай быстро и честно — люди сами к тебе придут.", "Don't listen to him. Work fast and honest — people will come to you."),
                    A("Накопишь денег — переезжай на рынок. Всё начнёшь заново, но станешь сильнее.", "Save up — then move to the market. You'll start over, but stronger."),
                };
                case 2: return new[]
                {
                    A("Рынок! Здесь торгуются за каждую копейку, зато народу — тьма.", "The market! People haggle over every coin, but the crowds are huge."),
                    M("Всё с нуля... Но рецепт-то остался при мне!", "Starting from zero... but I still have the recipe!"),
                };
                case 3: return new[]
                {
                    R("Говорят, у вас лучшая шаурма в районе? Докажите это на вокзале!", "They say you make the best shawarma around? Prove it at the station!"),
                    K("Вокзал — моя территория. Посмотрим, как ты выдержишь поток.", "The station is my turf. Let's see you handle the rush."),
                };
                case 4: return new[]
                {
                    K("Пляж мой! Мои киоски на каждом шагу!", "The beach is mine! My stands are everywhere!"),
                    M("Посмотрим, к кому пойдут отдыхающие.", "We'll see who the vacationers choose."),
                };
                case 5: return new[]
                {
                    A("Студенты голодные и честные. Понравится — расскажут всем.", "Students are hungry and honest. If they like it — they'll tell everyone."),
                };
                case 6: return new[]
                {
                    R("Финал кубка! Сто тысяч болельщиков и один ларёк. Справитесь?", "Cup final! A hundred thousand fans and one kiosk. Can you handle it?"),
                    M("Спортсменам — двойное мясо. Погнали!", "Double meat for the athletes. Let's go!"),
                };
                case 7: return new[]
                {
                    K("Ты зашёл слишком далеко. В аэропорту у меня контракт!", "You've gone too far. I have a contract at the airport!"),
                    A("Контракты — бумага. Вкус — навсегда.", "Contracts are paper. Taste is forever."),
                };
                case 8: return new[]
                {
                    R("Ночной центр: неон, такси и толпы после смены. Ночью шаурма нужна всем.", "Downtown at night: neon, taxis and crowds after the shift. At night everyone wants shawarma."),
                    M("Ночью ларьки мистера Меги закрыты. Весь город — наш!", "Mr. Mega's kiosks are closed at night. The whole city is ours!"),
                };
                case 9: return new[]
                {
                    K("Ладно... Ты хорош. Но фудкорт — это высшая лига.", "Fine... you're good. But the food court is the big league."),
                };
                case 10: return new[]
                {
                    A("Сынок... последний шаг. Отсюда видно весь город.", "Son... the last step. You can see the whole city from here."),
                    K("Небоскрёб? Мой офис этажом выше. Не мешай мне тут жарить!", "The skyscraper? My office is one floor up. Stop grilling under me!"),
                    M("Скоро это будет наш этаж, мистер Мега.", "Soon this will be our floor, Mr. Mega."),
                };
                case 11: return new[]
                {
                    R("Вас пригласили кормить правительство! Депутаты и министры — народ капризный.", "You've been invited to feed the government! Deputies and ministers are picky."),
                    K("Даже здесь мои связи... Министры едят только МегаШаурму!", "Even here I have connections... Ministers eat only MegaShawarma!"),
                    A("Министры тоже люди. Накорми их честно — и связи не понадобятся.", "Ministers are people too. Feed them honestly — no connections needed."),
                };
                case 12: return new[]
                {
                    A("Дубай, сынок! Здесь шейхи платят золотом за настоящий вкус.", "Dubai, son! Here sheikhs pay in gold for real taste."),
                    K("Это мой последний рубеж. Я построил здесь МегаБашню!", "This is my last stand. I built the MegaTower here!"),
                    M("Ещё немного — и миллиард наш!", "Just a bit more — and the billion is ours!"),
                };
                case 13: return new[]
                {
                    K("Невозможно... Миллиард?! Ты купил мою корпорацию?!", "Impossible... A billion?! You bought my corporation?!"),
                    A("Добро пожаловать домой, Магнат. Ресторан снова наш.", "Welcome home, Tycoon. The restaurant is ours again."),
                    M("Это только начало!", "This is just the beginning!"),
                };
            }
            return new StoryLine[0];
        }
    }
}
