using System.ComponentModel.DataAnnotations;

namespace Otzivi.ViewModels
{
    public class SetSecurityQuestionViewModel
    {
        [Required(ErrorMessage = "Выберите секретный вопрос")]
        [Display(Name = "Секретный вопрос")]
        public string SecurityQuestion { get; set; }

        [Required(ErrorMessage = "Введите ответ на секретный вопрос")]
        [Display(Name = "Ответ")]
        [StringLength(100, ErrorMessage = "Ответ должен быть от {2} до {1} символов", MinimumLength = 2)]
        public string SecurityAnswer { get; set; }

        [Display(Name = "Включить двухфакторную аутентификацию через секретный вопрос")]
        public bool Enable2FA { get; set; }

        // Список доступных вопросов
        public List<string> AvailableQuestions { get; set; } = new List<string>
        {
            "Девичья фамилия вашей матери?",
            "Имя вашего первого питомца?",
            "Название вашей первой школы?",
            "Город вашего рождения?",
            "Ваше любимое блюдо в детстве?",
            "Фамилия вашего лучшего друга детства?",
            "Марка вашей первой машины?",
            "Название вашей любимой книги?"
        };
    }
}