export interface MemberData{
    accountGuid: string,
    accountType: number,
    firstName: string,
    middleName: string,
    lastName: string,
    email: string,
    phoneNumber: string,
    mobileNumber: string,
    personalTrainerId: number,
    workoutGroupIds: Array<number>,
    workingExperienceInMonths: number,
    gymSubscriptionType: number
}