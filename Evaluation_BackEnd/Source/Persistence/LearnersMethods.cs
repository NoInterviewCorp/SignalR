using System;
using System.Collections.Generic;
using System.Linq;
using Evaluation_BackEnd.Models;
using Evaluation_BackEnd.RabbitMQModels;
using Evaluation_BackEnd.StaticData;
using Learners.Services;
using RabbitMQ.Client;

namespace Evaluation_BackEnd.Persistence {
    public class LearnersMethods : ITestMethods {
        private static QueueHandler queuehandler;
        public LearnersMethods (QueueHandler _queuehandler) {
            // graphclient = _graphclient;
            queuehandler = _queuehandler;
        }

        public void EvaluateAnswer (string username, string QuestionId, int OptionId) {
            var userdata = TemporaryQuizData.data[username];
            foreach (KeyValuePair<string, List<Question>> entry in userdata.QuestionsAttempted) {
                var question = entry.Value.FirstOrDefault (id => id.QuestionId == QuestionId);
                if (question != null) {
                    if (question.CorrectOption.OptionId == OptionId) {
                        TemporaryQuizData.data[username].ConceptsAttempted[entry.Key].QuestionAttemptedCorrect++;
                        TemporaryQuizData.data[username].ConceptsAttempted[entry.Key].TotalQuestionAttempted++;
                    } else {
                        TemporaryQuizData.data[username].ConceptsAttempted[entry.Key].TotalQuestionAttempted++;
                    }
                }
            }
        }
        public void SendEvaluationToGraph (string username, string concept, int bloom) {
            var requestdata = new ResultWrapper (username, concept, bloom);
            var serilaizeddata = requestdata.Serialize ();
            queuehandler.Model.BasicPublish ("KnowledgeGraphExchange", "Result.Update", null, serilaizeddata);
        }
        public void OnFinish (UserData data) {
            throw new NotImplementedException ();
        }

        public void OnStart (TemporaryData temp, string username) {
            TemporaryQuizData.data[username] = temp;
        }

        public void GetQuestionsBatch (string username, string tech, List<string> concepts) {
            Console.WriteLine ("---Interface method invoked---");
            try {
                var RequestData = new QuestionsBatchRequest (username, tech, concepts);
                var serializeddata = RequestData.Serialize ();
                queuehandler.Model.BasicPublish (exchange: "KnowledgeGraphExchange", 
                    routingKey: "Question.Batch", 
                    basicProperties : null, 
                    body : serializeddata);

            } catch (System.Exception e) {
                Console.WriteLine (e.Message);
                throw;
            }
        }

        public void GetQuestions (string username, string tech, string concept) {
            var temporary = new QuestionsRequest (username, tech, concept);
            var serializeddata = temporary.Serialize ();
            queuehandler.Model.BasicPublish ("KnowledgeExchange", "Routing key", null, serializeddata);
        }
    }
}