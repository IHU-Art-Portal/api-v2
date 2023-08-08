﻿# Documentation for Theatrical Plays Api

This document pertains to my thesis, which focuses on developing an API for managing theatrical play data.
Its primary objective is to support the implementation of a web-based, front end, an Android application, and an iOS application.
The API was built utilizing the .Net Framework and the C# programming language.

-------
## Api Response

Every server response is wrapped within the `ApiResponse` container object. 
This object comprises four possible fields: `data`, `success`, `message`, and the `errorCode`.

The `data` field contains the requested data.\
The `success` field indicates if the request was successful (boolean).\
The `message` field contains error or success messages of the request.\
The `errorCode` field is an enum that contains all possible errors.

Examples:\
<b>Successful Case:</b>
```json 
{
    "data": "some data",
    "success": true,
    "message": "Completed"
}
```
<b>Unsuccessful Case:</b>
```json
{
  "success": false,
  "message": "Person does not exist",
  "errorCode": "NotFound" 
}
```
-------
## Collaborative projects
The solution is divided into four essential projects, for better management.
1) Theatrical.Api
2) Theatrical.Data
3) Theatrical.Dto
4) Theatrical.Services

Each project contains related job classes.
1) `Theatrical.Api`: The start-up class of the project lives here. It configures the services and the settings of the web API, also contains the controllers.
2) `Theatrical.Data`: The data project holds the models, the database schema and configuration, and migrations (if any).
3) `Theatrical.Dto`: The Dto project contains all the Data Transfer Objects, and the `ApiResponse` template container.
4) `Theatrical.Services`: The Service project includes all the services used, such as the repository which is used to interact with the database.

<I>A project may reference one or more projects in order to use its functions and data</I>.\
For example `Theatrical.Api` has references to all three other project, as it will be using the `Dtos`, `Data`, and `Services` a lot.

-------

## Api Requests
<b>Changes from version 1 (https://github.com/ar1st/theatrical-plays-api/blob/master/documentation.md) project:</b>
<ul><li><b>/api/people/letter</b> endpoint has changed to <b>/api/people/initials/letters</b></li>
    <li><b>/api/people/initials/letters</b> functionality changed to return results based on the provided initials,
instead of one provided letter.</li>
</ul>
<I>New changes will be listed here.</I><br>
<I>This text will be removed when everything is implemented and working.</I>

## User

---
This controller manages all the requests regarding user registration, login, verify, balance.

---

This endpoint is used by the users to register.
It returns a `UserDtoRole` wrapped in `ApiResponse`.

**Register**

| POST                             | /api/user/register                                    |
|----------------------------------|-------------------------------------------------------|
| **Parameters**                   |                                                       |
| *RegisterUserDto*                | {string: Email, string: Password, int Role}           |
| **Responses**                    |                                                       |
| *ApiResponse&lt;UserDtoRole&gt;* | {int: Id, string: Email, bool: Enabled, string: Note} |

----

This endpoint is used to verify their email.
It returns a message wrapped in `ApiResponse`.

**Verify**

| GET            | /api/user/verify                                       |
|----------------|--------------------------------------------------------|
| **Parameters** |                                                        |
| *string*       | <u>Query parameter</u>                                 |
|                | *token* from user's email.                             |
| **Responses**  |                                                        |
| *ApiResponse*  | {Message field overrides with the appropriate message} |

---

**Login**

This endpoint is used to login.
It returns a `JwtDto` wrapped in `ApiResponse`.

| POST                  | /api/user/login                                             |
|-----------------------|-------------------------------------------------------------|
| **Parameters**        |                                                             |
| *LoginUserDto*        | <u>JSON object</u>                                          |
|                       | {string: Email, string Password}                            |
| **Responses**         |                                                             |
| *ApiResponse<JwtDto>* | {string: access_token, string: token_type, int: expires_in} |

---

**Balance**

This endpoint is used to find someone's balance (credits).
It returns a message wrapped in `ApiResponse`. The message overrides the data field.

| GET                   | /api/user/{id}/balance                    |
|-----------------------|-------------------------------------------|
| **Parameters**        |                                           |
| int                   | Id of the user                            |
| **Responses**         |                                           |
| *ApiResponse<string>* | {data field is overridden with a message} |


----

## Person

---
This controller manages all the requests regarding a person. 👤<br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>ApiResponse</code> 📦.

**Create Person**

| POST                           | /api/person                                                                |
|--------------------------------|----------------------------------------------------------------------------|
| **Parameters**                 |                                                                            |
| *CreatePersonDto*              | {string: FullName, List&lt;string&gt;: url of image (if any), int: System} |
| **Responses**                  |                                                                            |
| *ApiResponse&lt;PersonDto&gt;* | {int: Id, string: FullName, int: SystemID, DateTime: Timestamp}            |

----

**Get Person**

This request is used to retrieve a person. 👤<br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>ApiResponse</code> 📦.

| GET                     | /api/person/{id}                                                |
|-------------------------|-----------------------------------------------------------------|
| **Parameters**          |                                                                 |
| *id*                    | <u>Path variable</u>                                            |
|                         | The integer identifier of the performer to retrieve.            |
| **Responses**           |                                                                 |
| ApiResponse\<PersonDto> | {int: Id, string: FullName, int: SystemID, DateTime: Timestamp} |

---

**Get People**

This request is used to retrieve all people. 👥<br>
Pagination available. 📄<br>
<i>If neither of page and size is specified, all results are returned.</i><br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>PaginationResult</code> 📜 which is wrapped in <code>ApiResponse</code> 📦.

| GET                                                  | /api/people                                                     |
|------------------------------------------------------|-----------------------------------------------------------------|
| **Parameters**                                       |                                                                 |
| *page*                                               | <u>Query parameter</u>                                          |
|                                                      | The index of the page to return. Optional                       |
| *size*                                               | <u>Query parameter</u>                                          |
|                                                      | The size of the page. Optional                                  |
| **Responses**                                        |                                                                 |
| ApiResponse&lt;PaginationResult&lt;PersonDto&gt;&gt; | {List&lt;PersonDto&gt;: Results, int CurrentPage, int PageSize} |

---

**Get People By Role**

This request returns people filtered by the provided role. 👥<br>
Pagination available. 📄<br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>PaginationResult</code> 📜 which is wrapped in <code>ApiResponse</code> 📦.


| GET                                                  | /api/people/role/{role}                                         |
|------------------------------------------------------|-----------------------------------------------------------------|
| **Parameters**                                       |                                                                 |
| *role*                                               | <u>Path parameter</u>                                           |
|                                                      | The role provided to filter the results                         |
| *page*                                               | <u>Query parameter</u>                                          |
|                                                      | The index of the page to return. Optional                       |
| *size*                                               | <u>Query parameter</u>                                          |
|                                                      | The size of the page. Optional                                  |
| **Responses**                                        |                                                                 |
| ApiResponse&lt;PaginationResult&lt;PersonDto&gt;&gt; | {List&lt;PersonDto&gt;: Results, int CurrentPage, int PageSize} |

---

**Get People By Initial Letter**

This request returns people filtered by the provided role. 👥<br>
Pagination available. 📄<br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>PaginationResult</code> 📜 which is wrapped in <code>ApiResponse</code> 📦.


| GET                                                  | /api/people/initials/{letters}                                |
|------------------------------------------------------|---------------------------------------------------------------|
| **Parameters**                                       |                                                               |
| *letters*                                            | <u>Path parameter</u>                                         |
|                                                      | The initials of the name.                                     |
| *page*                                               | <u>Query parameter</u>                                        |
|                                                      | The index of the page to return. Optional                     |
| *size*                                               | <u>Query parameter</u>                                        |
|                                                      | The size of the page. Optional                                |
| **Responses**                                        |                                                               |
| ApiResponse&lt;PaginationResult&lt;PersonDto&gt;&gt; | List&lt;PersonDto&gt;: Results, int CurrentPage, int PageSize |

---

**Get Person Productions**

This request returns all the productions that one <code>Person</code> partakes in. 🎬<br>
Pagination available. 📄<br>
It returns a <code>PersonProductionsRoleInfo</code> 🧑‍🎬 wrapped in <code>PaginationResult</code> 📜 which is wrapped in <code>ApiResponse</code> 📦.


| GET                                                                  | /api/people/{id}/productions                                                  |
|----------------------------------------------------------------------|-------------------------------------------------------------------------------|
| **Parameters**                                                       |                                                                               |
| *id*                                                                 | <u>Path parameter</u>                                                         |
|                                                                      | The Id of a person.                                                           |
| **Responses**                                                        |                                                                               |
| ApiResponse&lt;PaginationResult&lt;PersonProductionsRoleInfo&gt;&gt; | List&lt;PersonProductionsRoleInfo&gt;: Results, int CurrentPage, int PageSize |

---

**Get Person Photos**

This request returns a <code>Person</code>'s photos. 📸<br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>PaginationResult</code> 📜 which is wrapped in <code>ApiResponse</code> 📦.


| GET                                     | /api/people/{id}/photos                 |
|-----------------------------------------|-----------------------------------------|
| **Parameters**                          |                                         |
| *id*                                    | <u>Path parameter</u>                   |
|                                         | The Id of a person.                     |
| **Responses**                           |                                         |
| ApiResponse&lt;List&lt;ImageDto&gt;&gt; | int: Id, string ImageIrl, int: PersonId |

---

**Delete Person**

This request 🔥 deletes 🔥 a Person by their ID. 👤<br>
Only available to Admin accounts 👑<br>
<span>⚠️ Use with caution ⚠️</span><br>
It returns a <code>PersonDto</code> 🧍 wrapped in <code>PaginationResult</code> 📜 which is wrapped in <code>ApiResponse</code> 📦.


| DELETE         | /api/people/{id}                        |
|----------------|-----------------------------------------|
| **Parameters** |                                         |
| *id*           | <u>Path parameter</u>                   |
|                | The Id of the person.                   |
| **Responses**  |                                         |
| ApiResponse    | message: overriden with success message |

----

# Logs

The `LogsController` is responsible for managing requests related to logs.

## Methods

| Method  | Endpoint                               |
|---------|----------------------------------------|
| **GET** | `/api/logs`                            |

### Get Logs

Retrieve a list of all logs with optional pagination.

| Parameter | Type            | Description                        |
|-----------|-----------------|------------------------------------|
| `page`    | Query parameter | Page number for pagination.        |
| `size`    | Query parameter | Number of items per page.          |

**Authorization:**
Requires admin authorization.

**Response:**

An `ApiResponse` containing a paginated list of `LogDto` objects wrapped in a `PaginationResult`.

----

# Events Controller

The `EventsController` is responsible for managing requests related to events.

## Methods

| Method   | Endpoint           |
|----------|--------------------|
| **GET**  | `/api/events`      |
| **POST** | `/api/events`      |

### Get Events

| Method   | Endpoint           |
|----------|--------------------|
| **GET**  | `/api/events`      |

Retrieve a list of all events with optional pagination.

| Parameter | Type            | Description                        |
|-----------|-----------------|------------------------------------|
| `page`    | Query parameter | Page number for pagination.        |
| `size`    | Query parameter | Number of items per page.          |

**Response:**

- If successful, returns an `ApiResponse` containing a paginated list of `EventDto` objects wrapped in a `PaginationResult`.
- If validation fails, returns an `ApiResponse` with an appropriate error message.

### Create Event


| Method   | Endpoint           |
|----------|--------------------|
| **POST** | `/api/events`      |

Create a new event.

| Parameter        | Type           | Description                  |
|------------------|----------------|------------------------------|
| `createEventDto` | Request body   | Data for creating the event. |

**Authorization:**
Requires admin authorization.

**Response:**

- If successful, returns an `ApiResponse` with a success message.
- If validation fails, returns an `ApiResponse` with an appropriate error message.

---