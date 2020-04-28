using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AI_TaskInterceptor : MonoBehaviour
{
    [SerializeField] GUISkin _skin;

    Tenant[] _tenants;

    List<PPTask> _inputTasks;
    List<PPTask> _mainTaskPool = new List<PPTask>();
    List<PPTask> _deniedTaskPool = new List<PPTask>();
    List<PPTask> _todayTaskPool;
    List<PPTask>[] _weekTaskPool = { new List<PPTask>(), new List<PPTask>(), new List<PPTask>(), new List<PPTask>(), new List<PPTask>(), new List<PPTask>(), new List<PPTask>() };

    Dictionary<PPTask, int> _taskOccurrencies = new Dictionary<PPTask, int>();

    string[] _daysNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    int _currentWeekDay = 0;

    int _day = 0;
    int _dayLearningCap = 50;
    float _dayStep = 0.05f; //in seconds
    List<List<string>> test = new List<List<string>>();

    int _systemScore = 0;

    void Start()
    {
        _inputTasks = JSONReader.ReadTasksAsList();
        string[] tenantsNames = CSVReader.ReadNames("Input Data/TENANT_NAMES");

        _tenants = new Tenant[tenantsNames.Length];

        StartCoroutine(DailyTasker());
    }

    // Update is called once per frame
    void Update()
    {
        //print(_mainTaskPool.Count);
    }

    IEnumerator DailyTasker()
    {
        while (_day < 999)
        {
            float dayProb = Random.value;
            _currentWeekDay++;
            if (_currentWeekDay > 6) _currentWeekDay = 0;
            _weekTaskPool[_currentWeekDay] = new List<PPTask>();

            _todayTaskPool = new List<PPTask>();
            List<PPTask> suggestedTasks =  DailySuggestTasks();


            foreach (var task in _inputTasks)
            {
                if (task.TaskDay == _currentWeekDay && task.TaskProbability >= dayProb)
                {
                    if (suggestedTasks.Contains(task))
                    {
                        task.AiAccepted = true;
                        task.AiSuggested = true;
                    }
                    _mainTaskPool.Add(task);
                    _todayTaskPool.Add(task);
                    _weekTaskPool[_currentWeekDay].Add(task);
                }                
            }
            var dailyDenied = suggestedTasks.Where(s => !_todayTaskPool.Contains(s));
            foreach (var dTask in dailyDenied)
            {
                dTask.AiAccepted = false;
                _mainTaskPool.Add(dTask);
                _todayTaskPool.Add(dTask);
                _weekTaskPool[_currentWeekDay].Add(dTask);
            }
            //_deniedTaskPool.Add();
            TaskCounter();
            foreach (var item in _todayTaskPool)
            {
                if (item.AiAccepted) _systemScore += 5;
                if (item.AiSuggested && !item.AiAccepted) _systemScore -= 1;

            }
            _day++;
            //StartCoroutine(SaveScreenshot());
            yield return new WaitForSeconds(_dayStep);
        }
    }

    void TaskCounter()
    {
        _taskOccurrencies = _mainTaskPool.Where(t => t.AiAccepted || !t.AiSuggested)
            .GroupBy(t => t.GetHashCode())
            .Select(group => group.ToList()).ToList()
            .ToDictionary(group => group.First(), group => group.Count); ;

        //foreach (var item in _taskOccurrencies)
        //{
        //    print($"Task: {item.Key.taskTitle} {item.Value} times");
        //}

    }

    List<PPTask> DailySuggestTasks()
    {
        List<PPTask> suggestedTasks = new List<PPTask>();
        float week = _day / 7f;
        foreach (var task in _taskOccurrencies)
        {
            float taskFrequency = task.Value / week;
            if (_day > _dayLearningCap && taskFrequency >= 0.65f)
            {
                if (task.Key.TaskDay == _currentWeekDay)
                {
                    var newTask = task.Key.CopyTask();


                    newTask.AiSuggested = true;
                    suggestedTasks.Add(newTask);
                }
                
            }
        }
        return suggestedTasks;
    }

    IEnumerator SaveScreenshot()
    {
        string file = $"SavedFrames/TaskInterceptor/Frame_{_day}.png";
        ScreenCapture.CaptureScreenshot(file);
        yield return new WaitForEndOfFrame();
    }


    private void OnGUI()
    {
        GUI.skin = _skin;

        //Logo
        GUI.DrawTexture(new Rect(20, -10, 128, 128), Resources.Load<Texture>("Textures/PP_Logo"));

        //Title
        GUI.Box(new Rect(180, 30, 500, 25), "AI Task Interceptor", "title");

        //Score counter
        GUI.Box(new Rect(700, 30, 500, 25), $"System Score: {_systemScore}", "title");

        //Day Counter
        GUI.Box(new Rect(Screen.width - 125, 30, 100, 25), $"Day: {_day}, {_daysNames[_currentWeekDay]}", "subtitle");

        //Day Panels
        var paddingA = 10;

        var dayBoxWidth = (Screen.width - (paddingA * (_daysNames.Length + 1))) / _daysNames.Length;
        int dayBoxHeight = 50;


        for (int i = 0; i < 7; i++)
        {
            //Day Panels
            Rect dayRect = new Rect((paddingA * (i + 1) + (dayBoxWidth * i)), 100, dayBoxWidth, dayBoxHeight);

            string dayBoxStyle;
            string taskBoxStyle;
            if (i == _currentWeekDay)
            {
                dayBoxStyle = "dayTitleActive";
                taskBoxStyle = "taskActive";
            }
            else
            {
                dayBoxStyle = "dayTitleInactive";
                taskBoxStyle = "taskInactive";
            }
            GUI.Box(dayRect, _daysNames[i], dayBoxStyle);



            //Tasks Panels
            var todaysTasks = _weekTaskPool[i];
            if (todaysTasks.Count == 0)
            {
                Rect taskRect = new Rect(dayRect.x, dayRect.yMax + paddingA, dayRect.width, dayBoxHeight);
                GUI.Box(taskRect, "No Tasks Today!", taskBoxStyle);
            }
            else
            {
                for (int j = 0; j < todaysTasks.Count; j++)
                {
                    var task = todaysTasks[j];
                    if (task.AiSuggested)
                    {
                        if (task.AiAccepted)
                        {
                            if (i == _currentWeekDay)
                            {
                                taskBoxStyle = "AItaskActiveAccepted";
                            }
                            else
                            {
                                taskBoxStyle = "AItaskInactiveAccepted";
                            }
                        }
                        else
                        {
                            if (i == _currentWeekDay)
                            {
                                taskBoxStyle = "AItaskActiveDenied";
                            }
                            else
                            {
                                taskBoxStyle = "AItaskInactiveDenied";
                            }
                        }
                    }
                    
                    Rect taskRect = new Rect(dayRect.x, (dayRect.yMax + paddingA) + ((dayBoxHeight + paddingA) * j), dayRect.width, dayBoxHeight);
                    GUIContent taskContent = new GUIContent();
                    taskContent.text = $"{task.TenantName} requested {task.TaskTitle} @ {task.TaskTime}h";
                    GUI.Box(taskRect, taskContent, taskBoxStyle);
                }
            }
        }
    }
}