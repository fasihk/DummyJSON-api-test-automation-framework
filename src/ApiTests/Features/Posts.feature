Feature: DummyJSON posts API
  As an API test author
  I want to validate post endpoints
  So that response status codes, payloads, and attributes are verified

  Background:
    Given DummyJSON is available

  Scenario: Retrieve a post by ID
    When I send a GET request to "/posts/1"
    Then the response status code should be 200
    And response JSON path "id" should equal "1"
    And response JSON path "title" should equal "His mother had always taught him"
    And response JSON path "userId" should equal "121"

  Scenario: Search posts by a term
    When I send a GET request to "/posts/search?q=love"
    Then the response status code should be 200
    And response JSON path "total" should be greater than 0
    And response JSON path "posts[0].body" should contain "love"

  Scenario: Filter and select post fields
    When I send a GET request to "/posts?limit=3&select=id,title"
    Then the response status code should be 200
    And response JSON path "limit" should equal "3"
    And response JSON path "posts[0].id" should equal "1"
    And response JSON path "posts[0].title" should equal "His mother had always taught him"
    And response JSON path "posts[0].body" should not exist

  Scenario: Add a post and validate the simulated response
    When I send a POST request to "/posts/add" with JSON body:
      """
      {
        "title": "Automation test post",
        "body": "Created by the API test suite",
        "userId": 1
      }
      """
    Then the response status code should be 201
    And response JSON path "id" should be greater than 0
    And response JSON path "title" should equal "Automation test post"
    And response JSON path "body" should equal "Created by the API test suite"
    And response JSON path "userId" should equal "1"

  Scenario: Update a post and validate the simulated response
    When I send a PUT request to "/posts/1" with JSON body:
      """
      {
        "title": "Updated by automation"
      }
      """
    Then the response status code should be 200
    And response JSON path "id" should equal "1"
    And response JSON path "title" should equal "Updated by automation"

  Scenario: Partially update a post and validate the simulated response
    When I send a PATCH request to "/posts/1" with JSON body:
      """
      {
        "title": "Patched by automation"
      }
      """
    Then the response status code should be 200
    And response JSON path "id" should equal "1"
    And response JSON path "title" should equal "Patched by automation"

  Scenario: Delete a post and validate the simulated response
    When I send a DELETE request to "/posts/1"
    Then the response status code should be 200
    And response JSON path "id" should equal "1"
    And response JSON path "isDeleted" should equal "true"
    And response JSON path "deletedOn" should exist