import {AccountTypeDropdown} from "./AccountTypeDropdown";
import {GymSubscriptionTypeDropdown} from "./GymSubscriptionTypeDropdown";

interface MemberData{
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

export function MemberInfoDashboard ({ userData }: { userData: MemberData }) {
    return (<div className="MemberHomePage">
        <label className="username">Username</label>
        <br/>
        <label className="username-email">{userData.email}</label>
        <br/>
        <label className="firstName">First name</label>
        <br/>
        <label className="firstName-data">{userData.firstName}</label>
        <br/>
        <label className="middleName">Middle name</label>
        <br/>
        <label className="middleName-data">{userData.middleName}</label>
        <br/>
        <label className="lastName">Last name</label>
        <br/>
        <label className="lastName-data">{userData.lastName}</label>
        <br/>
        <label className="phoneNumber">Phone number</label>
        <br/>
        <label className="phoneNumber-data">{userData.phoneNumber}</label>
        <br/>
        <label className="mobileNumber">Mobile phone</label>
        <br/>
        <label className="mobilePhone-data">{userData.mobileNumber}</label>
        <br/>
        <label className="gymSubscriptionType">Gym subscription: </label>
        <GymSubscriptionTypeDropdown userSubscriptionType={userData.gymSubscriptionType} />
        <br/>
        <label className="accountType">Account type: </label>
        <AccountTypeDropdown userAccountType={userData.accountType} />
    </div>)
}