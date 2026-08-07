import {axios} from "./schedulingHttpClient";
import {TrainerAvailability} from "../types/trainerAvailability";
import {InsertAvailability} from "../types/insertAvailability";
import {InsertWorkingHours} from "../types/insertWorkingHours";
import {TrainingSession} from "../types/trainingSession";
import {InsertTrainingSession} from "../types/insertTrainingSession";
import {TimeOff} from "../types/timeOff";
import {extractErrorMessage} from "../../../shared/api/extractErrorMessage";

export async function fetchAvailabilitiesForTrainer(trainerId: string): Promise<TrainerAvailability[]> {
    try {
        const response = await axios.get<TrainerAvailability[]>(`GetAvailabilitiesForTrainerId/${trainerId}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching trainer availability: ", error);
        return [];
    }
}

export async function addAvailability(insert: InsertAvailability): Promise<boolean> {
    try {
        await axios.post(`AddAvailability`, insert);
        return true;
    } catch (error) {
        console.error("Error creating trainer availability: ", error);
        return false;
    }
}

export async function addWorkingHoursToDailyAvailability(
    trainerId: string,
    dayName: string,
    hours: InsertWorkingHours[]
): Promise<boolean> {
    try {
        const response = await axios.post<boolean>(
            `AddWorkingHoursToDailyAvailability/${trainerId}/${dayName}/workinghours`,
            hours
        );
        return response.data === true;
    } catch (error) {
        console.error("Error adding working hours: ", error);
        return false;
    }
}

export async function updateAvailability(availability: TrainerAvailability): Promise<boolean> {
    try {
        await axios.put(`UpdateAvailability`, availability);
        return true;
    } catch (error) {
        console.error("Error updating trainer availability: ", error);
        return false;
    }
}

export async function deleteAvailability(id: string): Promise<boolean> {
    try {
        await axios.delete(`DeleteAvailability/${id}`);
        return true;
    } catch (error) {
        console.error("Error deleting trainer availability: ", error);
        return false;
    }
}

export async function fetchTrainingSessionsForClient(clientId: string): Promise<TrainingSession[]> {
    try {
        const response = await axios.get<TrainingSession[]>(`GetAllTrainingSessionsForClient/${clientId}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching training sessions: ", error);
        return [];
    }
}

export async function fetchTimeOffForTrainer(trainerId: string): Promise<TimeOff[]> {
    try {
        const response = await axios.get<TimeOff[]>(`GetAllForTrainerId/${trainerId}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching trainer time off: ", error);
        return [];
    }
}

export async function addTrainingSession(
    insert: InsertTrainingSession
): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.post(`AddTrainingSession`, insert);
        return { success: true };
    } catch (error) {
        console.error("Error booking training session: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't book this session.") };
    }
}
