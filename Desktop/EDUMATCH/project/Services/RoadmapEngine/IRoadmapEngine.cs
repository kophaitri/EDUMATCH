namespace EduMatch.Services.RoadmapEngine;

public interface IRoadmapEngine
{
    LearningRoadmap Generate(
        List<TopicAssessment> assessments,
        StudentProfile profile,
        int subjectId);
}
